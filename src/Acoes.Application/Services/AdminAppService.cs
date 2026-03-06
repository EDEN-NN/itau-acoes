using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Application.DTOs;
using Acoes.Application.Exceptions;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;

namespace Acoes.Application.Services;

public class AdminAppService
{
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminAppService(
        ICestaRecomendacaoRepository cestaRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICotacaoRepository cotacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _cestaRepository = cestaRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _cotacaoRepository = cotacaoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CestaResponse> CriarCestaAsync(CriarCestaRequest request)
    {
        if (request.Itens.Count != 5)
            throw new QuantidadeAtivosInvalidaException(request.Itens.Count);

        var somaPercentuais = request.Itens.Sum(i => i.Percentual);
        if (somaPercentuais != 100m)
            throw new PercentuaisInvalidosException(somaPercentuais);

        var cestaAnterior = await _cestaRepository.ObterCestaAtivaAsync();
        CestaAnteriorDto? cestaAnteriorDto = null;

        if (cestaAnterior is not null)
        {
            cestaAnterior.Desativar();
            await _cestaRepository.AtualizarAsync(cestaAnterior);
            cestaAnteriorDto = new CestaAnteriorDto
            {
                CestaId = cestaAnterior.Id,
                Nome = cestaAnterior.Nome,
                DataDesativacao = cestaAnterior.DataDesativacao!.Value
            };
        }

        var novaCesta = new CestaRecomendacao(request.Nome);
        await _cestaRepository.AdicionarAsync(novaCesta);
        await _unitOfWork.CommitAsync();

        foreach (var item in request.Itens)
            novaCesta.ItensCesta.Add(new ItemCesta(novaCesta.Id, item.Ticker.ToUpperInvariant(), item.Percentual));

        await _cestaRepository.AtualizarAsync(novaCesta);
        await _unitOfWork.CommitAsync();

        var rebalanceouAnterior = cestaAnterior is not null;

        var mensagem = rebalanceouAnterior
            ? $"Cesta atualizada. Rebalanceamento será disparado para os clientes ativos."
            : "Primeira cesta cadastrada com sucesso.";

        return MapearCestaResponse(novaCesta, rebalanceouAnterior, cestaAnteriorDto, mensagem);
    }

    
    public async Task<CestaResponse> ObterCestaAtivaAsync()
    {
        var cesta = await _cestaRepository.ObterCestaAtivaAsync()
            ?? throw new CestaNaoEncontradaException();

        return MapearCestaResponse(cesta, false);
    }

    
    public async Task<List<CestaResponse>> ObterHistoricoCestasAsync()
    {
        var cestas = await _cestaRepository.ObterHistoricoAsync();
        return cestas.Select(c => MapearCestaResponse(c, false)).ToList();
    }

    
    
    

    
    
    
    
    public async Task<InicializarSistemaResponse> InicializarSistemaAsync()
    {
        var contaMasterExistente = await _contaGraficaRepository.ObterContaMasterAsync();

        if (contaMasterExistente is not null)
            return new InicializarSistemaResponse
            {
                ContaMasterCriada = false,
                NumeroConta = contaMasterExistente.NumeroConta,
                Mensagem = $"Sistema já inicializado. Conta Master '{contaMasterExistente.NumeroConta}' ativa desde {contaMasterExistente.DataCriacao:dd/MM/yyyy}."
            };

        var contaMaster = new ContaGrafica(null, "MASTER-001", Domain.Enums.TipoContaGrafica.MASTER);
        await _contaGraficaRepository.AdicionarAsync(contaMaster);
        await _unitOfWork.CommitAsync();

        return new InicializarSistemaResponse
        {
            ContaMasterCriada = true,
            NumeroConta = contaMaster.NumeroConta,
            Mensagem = "Sistema inicializado com sucesso! Conta Master 'MASTER-001' criada."
        };
    }

    
    
    

    
    public async Task<ContaMasterResponse> ObterContaMasterAsync()
    {
        var contaMaster = await _contaGraficaRepository.ObterContaMasterAsync()
            ?? throw new DomainException("Conta Master não encontrada.", "CONTA_MASTER_NAO_ENCONTRADA", 404);

        var itens = new List<CustodiaItemDto>();

        foreach (var custodia in contaMaster.Custodias.Where(c => c.Quantidade > 0))
        {
            var cotacao = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(custodia.Ticker);
            var valorAtual = (cotacao?.PrecoFechamento ?? 0m) * custodia.Quantidade;

            itens.Add(new CustodiaItemDto
            {
                Ticker = custodia.Ticker,
                Quantidade = custodia.Quantidade,
                PrecoMedio = custodia.PrecoMedio,
                ValorAtual = Math.Round(valorAtual, 2)
            });
        }

        return new ContaMasterResponse
        {
            Id = contaMaster.Id,
            NumeroConta = contaMaster.NumeroConta,
            Tipo = contaMaster.Tipo.ToString(),
            Custodia = itens,
            ValorTotalResiduo = Math.Round(itens.Sum(i => i.ValorAtual), 2)
        };
    }

    
    
    

    private static CestaResponse MapearCestaResponse(
        CestaRecomendacao cesta,
        bool rebalanceamentoDisparado,
        CestaAnteriorDto? cestaAnterior = null,
        string? mensagem = null)
    {
        return new CestaResponse
        {
            CestaId = cesta.Id,
            Nome = cesta.Nome,
            Ativa = cesta.Ativa,
            DataCriacao = cesta.DataCriacao,
            DataDesativacao = cesta.DataDesativacao,
            Itens = cesta.ItensCesta.Select(i => new ItemCestaResponse
            {
                Ticker = i.Ticker,
                Percentual = i.Percentual
            }).ToList(),
            RebalanceamentoDisparado = rebalanceamentoDisparado,
            CestaAnteriorDesativada = cestaAnterior,
            Mensagem = mensagem
        };
    }
}
