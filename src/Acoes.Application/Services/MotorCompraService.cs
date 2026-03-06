using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Application.DTOs;
using Acoes.Application.Exceptions;
using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Acoes.Application.Services;

public class MotorCompraService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IOrdemCompraRepository _ordemCompraRepository;
    private readonly IDistribuicaoRepository _distribuicaoRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MotorCompraService> _logger;

    private const decimal AliquotaIRDedoDuro = 0.00005m;

    public MotorCompraService(
        IClienteRepository clienteRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICestaRecomendacaoRepository cestaRepository,
        ICotacaoRepository cotacaoRepository,
        IOrdemCompraRepository ordemCompraRepository,
        IDistribuicaoRepository distribuicaoRepository,
        IKafkaProducerService kafkaProducer,
        IUnitOfWork unitOfWork,
        ILogger<MotorCompraService> logger)
    {
        _clienteRepository = clienteRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cestaRepository = cestaRepository;
        _cotacaoRepository = cotacaoRepository;
        _ordemCompraRepository = ordemCompraRepository;
        _distribuicaoRepository = distribuicaoRepository;
        _kafkaProducer = kafkaProducer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExecutarCompraResponse> ExecutarCompraAsync(DateOnly dataReferencia)
    {
        var dataExecucao = dataReferencia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        _logger.LogInformation("Motor de compra iniciado para {Data}", dataReferencia);

        var (cesta, contaMaster, clientes) = await ValidarPreCondicoesAsync(dataExecucao, dataReferencia);

        var aportesPorCliente = clientes.ToDictionary(c => c.Id, c => Math.Round(c.ValorMensal / 3m, 2));
        var totalConsolidado = aportesPorCliente.Values.Sum();
        _logger.LogInformation("{N} clientes ativos — total consolidado R$ {Total}", clientes.Count, totalConsolidado);

        var resultadoPorAtivo = await ProcessarAtivosAsync(cesta, contaMaster, totalConsolidado);

        await _ordemCompraRepository.AdicionarVariosAsync(
            resultadoPorAtivo.Values.SelectMany(r => r.Ordens).ToList());
        await _unitOfWork.CommitAsync();

        var (distribuicoes, residuosPorTicker, totalEventosIR) =
            await DistribuirTodosAtivosAsync(resultadoPorAtivo, clientes, aportesPorCliente, totalConsolidado, contaMaster);

        await _unitOfWork.CommitAsync();
        _logger.LogInformation("Motor de compra concluído. {IR} eventos IR publicados.", totalEventosIR);

        return MontarResponse(dataExecucao, clientes.Count, totalConsolidado, resultadoPorAtivo, distribuicoes, residuosPorTicker, totalEventosIR);
    }

    private async Task<(CestaRecomendacao cesta, ContaGrafica contaMaster, List<Cliente> clientes)>
        ValidarPreCondicoesAsync(DateTime dataExecucao, DateOnly dataReferencia)
    {
        if (await _ordemCompraRepository.ExisteParaDataAsync(dataExecucao))
            throw new CompraJaExecutadaException(dataReferencia.ToString("yyyy-MM-dd"));

        var cesta = await _cestaRepository.ObterCestaAtivaAsync()
            ?? throw new CestaNaoEncontradaException();

        var contaMaster = await _contaGraficaRepository.ObterContaMasterAsync()
            ?? throw new DomainException("Conta Master não encontrada.", "CONTA_MASTER_NAO_ENCONTRADA", 404);

        var clientes = (await _clienteRepository.ObterAtivosAsync()).ToList();
        if (!clientes.Any())
            throw new DomainException("Não há clientes ativos para processar.", "SEM_CLIENTES_ATIVOS", 422);

        return (cesta, contaMaster, clientes);
    }

    private async Task<Dictionary<string, ResultadoAtivo>> ProcessarAtivosAsync(
        CestaRecomendacao cesta, ContaGrafica contaMaster, decimal totalConsolidado)
    {
        var resultado = new Dictionary<string, ResultadoAtivo>();

        foreach (var itemCesta in cesta.ItensCesta)
        {
            var ticker = itemCesta.Ticker;
            var valorAlocado = totalConsolidado * (itemCesta.Percentual / 100m);

            var cotacao = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(ticker);
            if (cotacao is null)
            {
                _logger.LogWarning("Cotação não encontrada para {Ticker} — ativo ignorado nesta rodada", ticker);
                continue;
            }

            var preco = cotacao.PrecoFechamento;
            var custodiamaster = await _custodiaRepository.ObterPorContaIdETickerAsync(contaMaster.Id, ticker);
            var saldoMaster = custodiamaster?.Quantidade ?? 0;
            var qtdBruta = (int)(valorAlocado / preco);
            var qtdAComprar = Math.Max(0, qtdBruta - saldoMaster);

            _logger.LogInformation("{Ticker}: valor={Valor:F2} cotação={Preco:F2} qtdBruta={B} saldoMaster={S} aComprar={C}",
                ticker, valorAlocado, preco, qtdBruta, saldoMaster, qtdAComprar);

            var (ordens, detalhes) = await CriarOrdensAsync(contaMaster.Id, ticker, qtdAComprar, preco);

            resultado[ticker] = new ResultadoAtivo
            {
                Ticker = ticker,
                Preco = preco,
                QtdDisponivel = qtdAComprar + saldoMaster,
                QtdComprada = qtdAComprar,
                SaldoMasterAntes = saldoMaster,
                Ordens = ordens,
                DetalhesOrdem = detalhes,
                Custodiamaster = custodiamaster
            };
        }

        return resultado;
    }

    private async Task<(List<OrdemCompra> ordens, List<DetalheOrdemDto> detalhes)> CriarOrdensAsync(
        long contaMasterId, string ticker, int qtdAComprar, decimal preco)
    {
        var ordens = new List<OrdemCompra>();
        var detalhes = new List<DetalheOrdemDto>();

        var lotePadrao = (qtdAComprar / 100) * 100;
        var fracionario = qtdAComprar % 100;

        if (lotePadrao > 0)
        {
            ordens.Add(new OrdemCompra(contaMasterId, ticker, lotePadrao, preco, TipoMercado.LOTE));
            detalhes.Add(new DetalheOrdemDto { Tipo = "LOTE_PADRAO", Ticker = ticker, Quantidade = lotePadrao });
            _logger.LogInformation("{Ticker}: ordem lote padrão — {Qtd} unidades", ticker, lotePadrao);
        }

        if (fracionario > 0)
        {
            var tickerFrac = ticker + "F";
            var cotacaoFrac = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(tickerFrac);
            var precoFrac = cotacaoFrac?.PrecoFechamento ?? preco;
            ordens.Add(new OrdemCompra(contaMasterId, tickerFrac, fracionario, precoFrac, TipoMercado.FRACIONARIO));
            detalhes.Add(new DetalheOrdemDto { Tipo = "FRACIONARIO", Ticker = tickerFrac, Quantidade = fracionario });
            _logger.LogInformation("{Ticker}: ordem fracionária — {Qtd} unidades", tickerFrac, fracionario);
        }

        return (ordens, detalhes);
    }

    private async Task<(List<DistribuicaoClienteDto> distribuicoes, Dictionary<string, int> residuos, int totalEventosIR)>
        DistribuirTodosAtivosAsync(
            Dictionary<string, ResultadoAtivo> resultadoPorAtivo,
            List<Cliente> clientes,
            Dictionary<long, decimal> aportesPorCliente,
            decimal totalConsolidado,
            ContaGrafica contaMaster)
    {
        var distribuicoesResponse = new List<DistribuicaoClienteDto>();
        var residuosPorTicker = new Dictionary<string, int>();
        var totalEventosIR = 0;

        foreach (var item in resultadoPorAtivo.Values)
        {
            if (item.QtdDisponivel <= 0) continue;

            var (distribuicoesAtivo, qtdDistribuidaTotal) =
                await CalcularDistribuicoesPorClienteAsync(item, clientes, aportesPorCliente, totalConsolidado);

            var eventosIR = await RegistrarDistribuicoesAsync(item, distribuicoesAtivo, aportesPorCliente, distribuicoesResponse);
            totalEventosIR += eventosIR;

            var residuo = item.QtdDisponivel - qtdDistribuidaTotal;
            residuosPorTicker[item.Ticker] = residuo;
            await AtualizarResiduoMasterAsync(item, contaMaster.Id, residuo);
        }

        return (distribuicoesResponse, residuosPorTicker, totalEventosIR);
    }

    private async Task<(List<(Cliente, long, int, Custodia)> distribuicoes, int qtdTotal)>
        CalcularDistribuicoesPorClienteAsync(
            ResultadoAtivo item,
            List<Cliente> clientes,
            Dictionary<long, decimal> aportesPorCliente,
            decimal totalConsolidado)
    {
        var distribuicoes = new List<(Cliente cliente, long contaFilhoteId, int qtd, Custodia custodiaFilhote)>();
        var qtdTotal = 0;

        foreach (var cliente in clientes)
        {
            var proporcao = aportesPorCliente[cliente.Id] / totalConsolidado;
            var qtdCliente = (int)(proporcao * item.QtdDisponivel);
            if (qtdCliente <= 0) continue;

            var contasFilhote = (await _contaGraficaRepository.ObterFilhotesPorClienteIdAsync(cliente.Id)).ToList();
            var contaFilhote = contasFilhote.FirstOrDefault();
            if (contaFilhote is null) continue;

            var custodiaFilhote = await ObterOuCriarCustodiaFilhoteAsync(contaFilhote.Id, item.Ticker);

            distribuicoes.Add((cliente, contaFilhote.Id, qtdCliente, custodiaFilhote));
            qtdTotal += qtdCliente;
        }

        return (distribuicoes, qtdTotal);
    }

    private async Task<Custodia> ObterOuCriarCustodiaFilhoteAsync(long contaFilhoteId, string ticker)
    {
        var custodia = await _custodiaRepository.ObterPorContaIdETickerAsync(contaFilhoteId, ticker);
        if (custodia is not null) return custodia;

        custodia = new Custodia(contaFilhoteId, ticker);
        await _custodiaRepository.AdicionarAsync(custodia);
        await _unitOfWork.CommitAsync();
        return custodia;
    }

    private async Task<int> RegistrarDistribuicoesAsync(
        ResultadoAtivo item,
        List<(Cliente cliente, long contaFilhoteId, int qtd, Custodia custodiaFilhote)> distribuicoes,
        Dictionary<long, decimal> aportesPorCliente,
        List<DistribuicaoClienteDto> distribuicoesResponse)
    {
        var totalEventosIR = 0;
        var ordemId = item.Ordens.FirstOrDefault()?.Id ?? 0;

        foreach (var (cliente, _, qtdCliente, custodiaFilhote) in distribuicoes)
        {
            var distribuicao = new Distribuicao(ordemId, custodiaFilhote.Id, item.Ticker, qtdCliente, item.Preco);
            await _distribuicaoRepository.AdicionarAsync(distribuicao);

            AtualizarPrecoMedio(custodiaFilhote, qtdCliente, item.Preco);
            await _custodiaRepository.AtualizarAsync(custodiaFilhote);

            await PublicarEventoIRAsync(cliente, item.Ticker, qtdCliente, item.Preco);
            totalEventosIR++;

            AcumularNaResponse(distribuicoesResponse, cliente, item.Ticker, qtdCliente, aportesPorCliente);
        }

        return totalEventosIR;
    }

    private static void AtualizarPrecoMedio(Custodia custodia, int qtdNova, decimal precoNovo)
    {
        var novoPrecoMedio = custodia.Quantidade == 0
            ? precoNovo
            : ((custodia.Quantidade * custodia.PrecoMedio) + (qtdNova * precoNovo)) / (custodia.Quantidade + qtdNova);

        custodia.Quantidade += qtdNova;
        custodia.PrecoMedio = Math.Round(novoPrecoMedio, 4);
    }

    private async Task PublicarEventoIRAsync(Cliente cliente, string ticker, int qtd, decimal preco)
    {
        var valorOperacao = qtd * preco;
        var valorIR = Math.Round(valorOperacao * AliquotaIRDedoDuro, 2);

        var evento = new EventoIR(cliente.Id, TipoEventoIR.DEDO_DURO, valorOperacao, valorIR);
        await _kafkaProducer.PublicarEventoIRAsync(evento);
        evento.MarcarComoPublicado();

        _logger.LogInformation("IR Dedo-Duro: cliente {Id} — {Ticker} {Qtd}x{Preco:F2} = R${Val:F2} IR={IR:F4}",
            cliente.Id, ticker, qtd, preco, valorOperacao, valorIR);
    }

    private static void AcumularNaResponse(
        List<DistribuicaoClienteDto> distribuicoesResponse,
        Cliente cliente,
        string ticker,
        int qtdCliente,
        Dictionary<long, decimal> aportesPorCliente)
    {
        var dto = distribuicoesResponse.FirstOrDefault(d => d.ClienteId == cliente.Id);
        if (dto is null)
        {
            dto = new DistribuicaoClienteDto
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                ValorAporte = aportesPorCliente[cliente.Id]
            };
            distribuicoesResponse.Add(dto);
        }
        dto.Ativos.Add(new AtivoDistribuidoDto { Ticker = ticker, Quantidade = qtdCliente });
    }

    private async Task AtualizarResiduoMasterAsync(ResultadoAtivo item, long contaMasterId, int residuo)
    {
        if (item.Custodiamaster is not null)
        {
            item.Custodiamaster.Quantidade = residuo;
            await _custodiaRepository.AtualizarAsync(item.Custodiamaster);
        }
        else if (residuo > 0)
        {
            var novaCustMaster = new Custodia(contaMasterId, item.Ticker) { Quantidade = residuo };
            await _custodiaRepository.AdicionarAsync(novaCustMaster);
        }
    }

    private static ExecutarCompraResponse MontarResponse(
        DateTime dataExecucao,
        int totalClientes,
        decimal totalConsolidado,
        Dictionary<string, ResultadoAtivo> resultadoPorAtivo,
        List<DistribuicaoClienteDto> distribuicoes,
        Dictionary<string, int> residuosPorTicker,
        int totalEventosIR)
    {
        var ordensDto = resultadoPorAtivo.Values.Select(r => new OrdemCompraDto
        {
            Ticker = r.Ticker,
            QuantidadeTotal = r.QtdComprada,
            PrecoUnitario = r.Preco,
            ValorTotal = Math.Round(r.QtdComprada * r.Preco, 2),
            Detalhes = r.DetalhesOrdem
        }).ToList();

        var residuosDto = residuosPorTicker
            .Where(kv => kv.Value > 0)
            .Select(kv => new ResiduoMasterDto { Ticker = kv.Key, Quantidade = kv.Value })
            .ToList();

        return new ExecutarCompraResponse
        {
            DataExecucao = dataExecucao,
            TotalClientes = totalClientes,
            TotalConsolidado = totalConsolidado,
            OrdensCompra = ordensDto,
            Distribuicoes = distribuicoes,
            ResiduosCustMaster = residuosDto,
            EventosIRPublicados = totalEventosIR,
            Mensagem = $"Compra programada executada com sucesso para {totalClientes} clientes."
        };
    }

    private class ResultadoAtivo
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int QtdDisponivel { get; set; }
        public int QtdComprada { get; set; }
        public int SaldoMasterAntes { get; set; }
        public List<OrdemCompra> Ordens { get; set; } = new();
        public List<DetalheOrdemDto> DetalhesOrdem { get; set; } = new();
        public Custodia? Custodiamaster { get; set; }
    }
}
