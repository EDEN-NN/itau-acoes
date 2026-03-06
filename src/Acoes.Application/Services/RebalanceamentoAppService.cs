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

public class RebalanceamentoAppService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IEventoIRRepository _eventoIRRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RebalanceamentoAppService> _logger;

    private const decimal LimiteIsencaoIR = 20_000m;
    private const decimal AliquotaIRVenda = 0.20m;

    public RebalanceamentoAppService(
        IClienteRepository clienteRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICestaRecomendacaoRepository cestaRepository,
        ICotacaoRepository cotacaoRepository,
        IEventoIRRepository eventoIRRepository,
        IKafkaProducerService kafkaProducer,
        IUnitOfWork unitOfWork,
        ILogger<RebalanceamentoAppService> logger)
    {
        _clienteRepository = clienteRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cestaRepository = cestaRepository;
        _cotacaoRepository = cotacaoRepository;
        _eventoIRRepository = eventoIRRepository;
        _kafkaProducer = kafkaProducer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RebalancearResponse> ExecutarRebalanceamentoAsync(RebalancearRequest request)
    {
        var (tickersNovos, tickersSairam, tickersEntraram, tickersMudaram) =
            await CarregarCestasECalcularDiffAsync(request);

        var clientes = (await _clienteRepository.ObterAtivosAsync()).ToList();
        if (!clientes.Any())
            throw new DomainException("Não há clientes ativos para rebalancear.", "SEM_CLIENTES_ATIVOS", 422);

        var response = new RebalancearResponse { TotalClientes = clientes.Count };

        foreach (var cliente in clientes)
            await RebalancearClienteAsync(cliente, tickersNovos, tickersSairam, tickersEntraram, tickersMudaram, response);

        await _unitOfWork.CommitAsync();

        response.Mensagem = $"Rebalanceamento concluído para {clientes.Count} clientes. " +
                            $"{response.VendasRealizadas.Count} vendas, {response.ComprasRealizadas.Count} compras.";
        return response;
    }

    private async Task<(Dictionary<string, decimal> tickersNovos,
                        HashSet<string> tickersSairam,
                        HashSet<string> tickersEntraram,
                        HashSet<string> tickersMudaram)>
        CarregarCestasECalcularDiffAsync(RebalancearRequest request)
    {
        var todasCestas = (await _cestaRepository.ObterHistoricoAsync()).ToList();

        var cestaAnterior = todasCestas.FirstOrDefault(c => c.Id == request.CestaAnteriorId)
            ?? throw new DomainException($"Cesta anterior {request.CestaAnteriorId} não encontrada.", "CESTA_NAO_ENCONTRADA", 404);

        var cestaNova = todasCestas.FirstOrDefault(c => c.Id == request.CestaNovaId)
            ?? throw new CestaNaoEncontradaException();

        var tickersAntigos = cestaAnterior.ItensCesta.ToDictionary(i => i.Ticker, i => i.Percentual);
        var tickersNovos = cestaNova.ItensCesta.ToDictionary(i => i.Ticker, i => i.Percentual);

        var tickersSairam = tickersAntigos.Keys.Except(tickersNovos.Keys).ToHashSet();
        var tickersEntraram = tickersNovos.Keys.Except(tickersAntigos.Keys).ToHashSet();
        var tickersMudaram = tickersNovos.Keys
            .Where(t => tickersAntigos.ContainsKey(t) && tickersAntigos[t] != tickersNovos[t])
            .ToHashSet();

        _logger.LogInformation("Rebalanceamento: saíram {S} ativos, entraram {E}, mudaram % {M}",
            tickersSairam.Count, tickersEntraram.Count, tickersMudaram.Count);

        return (tickersNovos, tickersSairam, tickersEntraram, tickersMudaram);
    }

    private async Task RebalancearClienteAsync(
        Cliente cliente,
        Dictionary<string, decimal> tickersNovos,
        HashSet<string> tickersSairam,
        HashSet<string> tickersEntraram,
        HashSet<string> tickersMudaram,
        RebalancearResponse response)
    {
        var contas = (await _contaGraficaRepository.ObterFilhotesPorClienteIdAsync(cliente.Id)).ToList();
        var conta = contas.FirstOrDefault();
        if (conta is null) return;

        var custodias = (await _custodiaRepository.ObterPorContaIdAsync(conta.Id))
            .ToDictionary(c => c.Ticker, c => c);

        var (valorObtidoNasVendas, lucroLiquidoTotal, totalVendasMes) =
            await VenderAtivosSaidosAsync(cliente, tickersSairam, custodias, response);

        var valorParaComprarNovos = valorObtidoNasVendas;
        (valorParaComprarNovos, lucroLiquidoTotal, totalVendasMes) = await RebalancearAtivosComMudancaAsync(
            cliente, tickersMudaram, tickersNovos, custodias, valorParaComprarNovos, lucroLiquidoTotal, totalVendasMes, response);

        await ComprarNovosAtivosAsync(cliente, conta.Id, tickersEntraram, tickersNovos, valorParaComprarNovos, response);

        await ApurarIRVendasAsync(cliente, totalVendasMes, lucroLiquidoTotal, response);
    }

    private async Task<(decimal valorObtido, decimal lucroTotal, decimal totalVendas)> VenderAtivosSaidosAsync(
        Cliente cliente,
        HashSet<string> tickersSairam,
        Dictionary<string, Custodia> custodias,
        RebalancearResponse response)
    {
        decimal valorObtido = 0m, lucroTotal = 0m, totalVendas = 0m;

        foreach (var ticker in tickersSairam)
        {
            if (!custodias.TryGetValue(ticker, out var cust) || cust.Quantidade <= 0) continue;

            var cotacao = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(ticker);
            var precoVenda = cotacao?.PrecoFechamento ?? cust.PrecoMedio;
            var valorVenda = cust.Quantidade * precoVenda;
            var lucro = cust.Quantidade * (precoVenda - cust.PrecoMedio);

            valorObtido += valorVenda;
            lucroTotal += lucro;
            totalVendas += valorVenda;

            _logger.LogInformation("VENDA saída: cliente {C} — {T} {Q}x{P:F2} lucro={L:F2}",
                cliente.Id, ticker, cust.Quantidade, precoVenda, lucro);

            response.VendasRealizadas.Add(CriarOperacaoDto(cliente.Id, ticker, cust.Quantidade, precoVenda, "VENDA"));

            cust.Quantidade = 0;
            await _custodiaRepository.AtualizarAsync(cust);
        }

        return (valorObtido, lucroTotal, totalVendas);
    }

    private async Task<(decimal valorParaComprar, decimal lucroTotal, decimal totalVendas)> RebalancearAtivosComMudancaAsync(
        Cliente cliente,
        HashSet<string> tickersMudaram,
        Dictionary<string, decimal> tickersNovos,
        Dictionary<string, Custodia> custodias,
        decimal valorParaComprar,
        decimal lucroTotal,
        decimal totalVendas,
        RebalancearResponse response)
    {
        var valorCarteira = await CalcularValorCarteiraAsync(custodias, tickersNovos);

        foreach (var ticker in tickersMudaram)
        {
            if (!custodias.TryGetValue(ticker, out var cust) || cust.Quantidade <= 0) continue;

            var cotacao = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(ticker);
            var precoAtual = cotacao?.PrecoFechamento ?? cust.PrecoMedio;
            var valorAtual = cust.Quantidade * precoAtual;
            var valorAlvo = valorCarteira * (tickersNovos[ticker] / 100m);

            if (valorAtual > valorAlvo)
            {
                var qtdVender = (int)((valorAtual - valorAlvo) / precoAtual);
                if (qtdVender <= 0) continue;

                var valorVendido = qtdVender * precoAtual;
                valorParaComprar += valorVendido;
                lucroTotal += qtdVender * (precoAtual - cust.PrecoMedio);
                totalVendas += valorVendido;

                _logger.LogInformation("VENDA rebalanceamento: cliente {C} — {T} {Q}x{P:F2}",
                    cliente.Id, ticker, qtdVender, precoAtual);

                response.VendasRealizadas.Add(CriarOperacaoDto(cliente.Id, ticker, qtdVender, precoAtual, "VENDA"));

                cust.Quantidade -= qtdVender;
                await _custodiaRepository.AtualizarAsync(cust);
            }
            else if (valorAlvo > valorAtual)
            {
                var disponivel = Math.Min(valorAlvo - valorAtual, valorParaComprar);
                var qtdComprar = (int)(disponivel / precoAtual);
                if (qtdComprar <= 0) continue;

                valorParaComprar -= qtdComprar * precoAtual;
                AtualizarPrecoMedio(cust, qtdComprar, precoAtual);
                await _custodiaRepository.AtualizarAsync(cust);

                response.ComprasRealizadas.Add(CriarOperacaoDto(cliente.Id, ticker, qtdComprar, precoAtual, "COMPRA"));
            }
        }

        return (valorParaComprar, lucroTotal, totalVendas);
    }

    private async Task ComprarNovosAtivosAsync(
        Cliente cliente,
        long contaId,
        HashSet<string> tickersEntraram,
        Dictionary<string, decimal> tickersNovos,
        decimal valorDisponivel,
        RebalancearResponse response)
    {
        if (!tickersEntraram.Any() || valorDisponivel <= 0) return;

        var totalPctNovos = tickersEntraram.Sum(t => tickersNovos[t]);

        foreach (var ticker in tickersEntraram)
        {
            var valorAlocar = valorDisponivel * (tickersNovos[ticker] / totalPctNovos);

            var cotacao = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(ticker);
            if (cotacao is null)
            {
                _logger.LogWarning("Cotação não encontrada para novo ativo {T} — ignorado", ticker);
                continue;
            }

            var preco = cotacao.PrecoFechamento;
            var qtdComprar = (int)(valorAlocar / preco);
            if (qtdComprar <= 0) continue;

            var custodia = await ObterOuCriarCustodiaAsync(contaId, ticker);
            AtualizarPrecoMedio(custodia, qtdComprar, preco);
            await _custodiaRepository.AtualizarAsync(custodia);

            _logger.LogInformation("COMPRA novo ativo: cliente {C} — {T} {Q}x{P:F2}",
                cliente.Id, ticker, qtdComprar, preco);

            response.ComprasRealizadas.Add(CriarOperacaoDto(cliente.Id, ticker, qtdComprar, preco, "COMPRA"));
        }
    }

    private async Task ApurarIRVendasAsync(
        Cliente cliente,
        decimal totalVendasMes,
        decimal lucroLiquidoTotal,
        RebalancearResponse response)
    {
        if (totalVendasMes <= 0) return;

        var agora = DateTime.UtcNow;
        var vendasMesAnt = await _eventoIRRepository.ObterVendasDoMesAsync(cliente.Id, agora.Year, agora.Month);
        var totalVendasAcumulado = totalVendasMes + vendasMesAnt.Sum(v => v.ValorBase);

        if (totalVendasAcumulado > LimiteIsencaoIR && lucroLiquidoTotal > 0)
        {
            var valorIR = Math.Round(lucroLiquidoTotal * AliquotaIRVenda, 2);
            var eventoIR = new EventoIR(cliente.Id, TipoEventoIR.IR_VENDA, lucroLiquidoTotal, valorIR);

            await _eventoIRRepository.AdicionarAsync(eventoIR);
            await _kafkaProducer.PublicarEventoIRAsync(eventoIR);
            eventoIR.MarcarComoPublicado();
            response.EventosIRPublicados++;

            _logger.LogInformation("IR_VENDA: cliente {C} — total vendas mês R${V:F2} lucro R${L:F2} IR R${IR:F2}",
                cliente.Id, totalVendasAcumulado, lucroLiquidoTotal, valorIR);
        }
        else
        {
            _logger.LogInformation("IR isento: cliente {C} — total vendas mês R${V:F2} (≤ R$20k ou prejuízo)",
                cliente.Id, totalVendasAcumulado);
        }
    }

    private async Task<decimal> CalcularValorCarteiraAsync(
        Dictionary<string, Custodia> custodias,
        Dictionary<string, decimal> tickersNovos)
    {
        var valor = 0m;
        foreach (var kv in custodias.Where(c => c.Value.Quantidade > 0 && tickersNovos.ContainsKey(c.Key)))
        {
            var cot = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(kv.Key);
            valor += kv.Value.Quantidade * (cot?.PrecoFechamento ?? kv.Value.PrecoMedio);
        }
        return valor;
    }

    private async Task<Custodia> ObterOuCriarCustodiaAsync(long contaId, string ticker)
    {
        var custodia = await _custodiaRepository.ObterPorContaIdETickerAsync(contaId, ticker);
        if (custodia is not null) return custodia;

        custodia = new Custodia(contaId, ticker);
        await _custodiaRepository.AdicionarAsync(custodia);
        await _unitOfWork.CommitAsync();
        return custodia;
    }

    private static void AtualizarPrecoMedio(Custodia custodia, int qtdNova, decimal precoNovo)
    {
        var novoPrecoMedio = custodia.Quantidade == 0
            ? precoNovo
            : Math.Round((custodia.Quantidade * custodia.PrecoMedio + qtdNova * precoNovo) / (custodia.Quantidade + qtdNova), 4);

        custodia.Quantidade += qtdNova;
        custodia.PrecoMedio = novoPrecoMedio;
    }

    private static OperacaoRebalanceamentoDto CriarOperacaoDto(
        long clienteId, string ticker, int quantidade, decimal preco, string tipo) =>
        new()
        {
            ClienteId = clienteId,
            Ticker = ticker,
            Quantidade = quantidade,
            PrecoUnitario = preco,
            ValorTotal = Math.Round(quantidade * preco, 2),
            Tipo = tipo
        };
}
