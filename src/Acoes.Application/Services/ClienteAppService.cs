using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Application.DTOs;
using Acoes.Application.Exceptions;
using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using Acoes.Domain.Interfaces.Repositories;

namespace Acoes.Application.Services;

public class ClienteAppService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteAppService(
        IClienteRepository clienteRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICestaRecomendacaoRepository cestaRepository,
        ICotacaoRepository cotacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cestaRepository = cestaRepository;
        _cotacaoRepository = cotacaoRepository;
        _unitOfWork = unitOfWork;
    }


    public async Task<AdesaoResponse> AderirAsync(AdesaoRequest request)
    {
        var clienteExistente = await _clienteRepository.ObterPorCpfAsync(request.Cpf);
        if (clienteExistente is not null)
            throw new ClienteCpfDuplicadoException(request.Cpf);

        if (request.ValorMensal < 100m)
            throw new ValorMensalInvalidoException();

        var cliente = new Cliente(request.Nome, request.Cpf, request.Email, request.ValorMensal);
        await _clienteRepository.AdicionarAsync(cliente);

        await _unitOfWork.CommitAsync();

        var numeroConta = GerarNumeroConta(cliente.Id);
        var contaGrafica = new ContaGrafica(cliente.Id, numeroConta, TipoContaGrafica.FILHOTE);
        await _contaGraficaRepository.AdicionarAsync(contaGrafica);

        await _unitOfWork.CommitAsync();

        var cestaAtiva = await _cestaRepository.ObterCestaAtivaAsync();
        if (cestaAtiva is not null)
        {
            foreach (var item in cestaAtiva.ItensCesta)
            {
                var custodia = new Custodia(contaGrafica.Id, item.Ticker);
                await _custodiaRepository.AdicionarAsync(custodia);
            }
            await _unitOfWork.CommitAsync();
        }

        return new AdesaoResponse
        {
            ClienteId = cliente.Id,
            NumeroConta = numeroConta,
            Mensagem = $"Adesão realizada com sucesso! Conta Gráfica {numeroConta} criada."
        };
    }


    public async Task AlterarValorMensalAsync(long clienteId, decimal novoValor)
    {
        var cliente = await ObterClienteOuThrowAsync(clienteId);

        if (novoValor < 100m)
            throw new ValorMensalInvalidoException();

        cliente.ValorMensal = novoValor;
        await _clienteRepository.AtualizarAsync(cliente);
        await _unitOfWork.CommitAsync();
    }


    public async Task SairDoProdutoAsync(long clienteId)
    {
        var cliente = await ObterClienteOuThrowAsync(clienteId);

        if (!cliente.Ativo)
            throw new ClienteJaInativoException(clienteId);

        cliente.Ativo = false;
        await _clienteRepository.AtualizarAsync(cliente);
        await _unitOfWork.CommitAsync();
    }


    public async Task<CarteiraResponse> ObterCarteiraAsync(long clienteId)
    {
        var cliente = await ObterClienteOuThrowAsync(clienteId);

        var contas = await _contaGraficaRepository.ObterFilhotesPorClienteIdAsync(clienteId);
        var conta = contas.FirstOrDefault()
            ?? throw new DomainException("Conta gráfica não encontrada para o cliente.", "CONTA_NAO_ENCONTRADA", 404);

        var custodias = (await _custodiaRepository.ObterPorContaIdAsync(conta.Id))
            .Where(c => c.Quantidade > 0)
            .ToList();

        var posicoes = new List<CarteiraPosicaoDto>();

        foreach (var custodia in custodias)
        {
            var cotacao = await _cotacaoRepository.ObterMaisRecentePorTickerAsync(custodia.Ticker);
            var cotacaoAtual = cotacao?.PrecoFechamento ?? 0m;

            var valorInvestido = custodia.Quantidade * custodia.PrecoMedio;
            var valorAtual = custodia.Quantidade * cotacaoAtual;
            var pl = valorAtual - valorInvestido;
            var plPercent = valorInvestido > 0 ? (pl / valorInvestido) * 100m : 0m;

            posicoes.Add(new CarteiraPosicaoDto
            {
                Ticker = custodia.Ticker,
                Quantidade = custodia.Quantidade,
                PrecoMedio = custodia.PrecoMedio,
                CotacaoAtual = cotacaoAtual,
                ValorAtual = valorAtual,
                PL = pl,
                PLPercent = Math.Round(plPercent, 2),
                PercentualCarteira = 0m
            });
        }

        var valorAtualTotal = posicoes.Sum(p => p.ValorAtual);
        var valorInvestidoTotal = custodias.Sum(c => c.Quantidade * c.PrecoMedio);

        if (valorAtualTotal > 0)
        {
            foreach (var posicao in posicoes)
                posicao.PercentualCarteira = Math.Round((posicao.ValorAtual / valorAtualTotal) * 100m, 2);
        }

        var plTotal = valorAtualTotal - valorInvestidoTotal;
        var rentabilidade = valorInvestidoTotal > 0
            ? Math.Round((plTotal / valorInvestidoTotal) * 100m, 2)
            : 0m;

        return new CarteiraResponse
        {
            ClienteId = clienteId,
            NomeCliente = cliente.Nome,
            ValorInvestidoTotal = Math.Round(valorInvestidoTotal, 2),
            ValorAtualTotal = Math.Round(valorAtualTotal, 2),
            PLTotal = Math.Round(plTotal, 2),
            RentabilidadePercent = rentabilidade,
            Posicoes = posicoes
        };
    }


    private async Task<Cliente> ObterClienteOuThrowAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId);
        if (cliente is null)
            throw new ClienteNaoEncontradoException(clienteId);
        return cliente;
    }

    private static string GerarNumeroConta(long clienteId)
    {
        return $"CTA-{clienteId:D8}";
    }
}
