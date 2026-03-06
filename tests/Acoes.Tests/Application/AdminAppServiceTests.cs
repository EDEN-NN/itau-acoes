using Acoes.Application.DTOs;
using Acoes.Application.Exceptions;
using Acoes.Application.Services;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace Acoes.Tests.Application;

public class AdminAppServiceTests
{
    // ── Fakes e sut ──────────────────────────────────────────────────────────

    private readonly Mock<ICestaRecomendacaoRepository> _cestaRepo = new();
    private readonly Mock<IContaGraficaRepository> _contaGraficaRepo = new();
    private readonly Mock<ICotacaoRepository> _cotacaoRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AdminAppService CriarSut() => new(
        _cestaRepo.Object,
        _contaGraficaRepo.Object,
        _cotacaoRepo.Object,
        _unitOfWork.Object);

    private static List<ItemCestaRequest> CriarItensCestaValidos(int quantidade = 5, decimal somaPct = 100m)
    {
        if (quantidade <= 0) return new List<ItemCestaRequest>();
        var basePct = somaPct / quantidade;
        return Enumerable.Range(1, quantidade)
            .Select(i => new ItemCestaRequest { Ticker = $"TICK{i}", Percentual = basePct })
            .ToList();
    }

    // ────────────────────────────────────────────────────────────────────────
    // CriarCestaAsync — RN-014 a RN-019
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CriarCestaAsync_DeveCriarCestaComSucesso_QuandoDadosValidos()
    {
        // Arrange
        _cestaRepo.Setup(r => r.ObterCestaAtivaAsync()).ReturnsAsync((CestaRecomendacao?)null);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(true);

        var request = new CriarCestaRequest
        {
            Nome = "Top Five Jan",
            Itens = CriarItensCestaValidos(5, 100m)
        };

        // Act
        var resultado = await CriarSut().CriarCestaAsync(request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Nome.Should().Be("Top Five Jan");
        resultado.Ativa.Should().BeTrue();
        resultado.Itens.Should().HaveCount(5);
        resultado.RebalanceamentoDisparado.Should().BeFalse();
        _cestaRepo.Verify(r => r.AdicionarAsync(It.IsAny<CestaRecomendacao>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]  // nenhum
    [InlineData(4)]  // menos de 5
    [InlineData(6)]  // mais de 5
    public async Task CriarCestaAsync_DeveLancarQuantidadeAtivosInvalidaException_QuandoNaoTiverCincoAtivos(int n)
    {
        // Arrange
        var request = new CriarCestaRequest
        {
            Nome = "Inválida",
            Itens = CriarItensCestaValidos(n, 100m)
        };

        // Act
        var act = async () => await CriarSut().CriarCestaAsync(request);

        // Assert
        await act.Should().ThrowAsync<QuantidadeAtivosInvalidaException>();
    }

    [Theory]
    [InlineData(99)]    // abaixo de 100
    [InlineData(101)]   // acima de 100
    [InlineData(85.5)]  // fracionado errado
    public async Task CriarCestaAsync_DeveLancarPercentuaisInvalidosException_QuandoSomaNaoFor100Pct(decimal soma)
    {
        // Arrange — 5 itens com soma diferente de 100
        var itens = Enumerable.Range(1, 5)
            .Select((i, idx) => new ItemCestaRequest
            {
                Ticker = $"TICK{i}",
                Percentual = idx == 0 ? soma - (4 * (soma / 5m)) : soma / 5m
            })
            .ToList();
        // Forçar soma errada diretamente
        var request = new CriarCestaRequest
        {
            Nome = "Inválida",
            Itens = new List<ItemCestaRequest>
            {
                new() { Ticker = "A", Percentual = 20 },
                new() { Ticker = "B", Percentual = 20 },
                new() { Ticker = "C", Percentual = 20 },
                new() { Ticker = "D", Percentual = 20 },
                new() { Ticker = "E", Percentual = soma - 80m } // faz a soma ser `soma`
            }
        };

        // Act
        var act = async () => await CriarSut().CriarCestaAsync(request);

        // Assert
        await act.Should().ThrowAsync<PercentuaisInvalidosException>();
    }

    [Fact]
    public async Task CriarCestaAsync_DeveDesativarCestaAnterior_QuandoJaExistirCestaAtiva()
    {
        // Arrange
        var cestaExistente = new CestaRecomendacao("Antiga");
        _cestaRepo.Setup(r => r.ObterCestaAtivaAsync()).ReturnsAsync(cestaExistente);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(true);

        var request = new CriarCestaRequest
        {
            Nome = "Nova Top Five",
            Itens = CriarItensCestaValidos(5, 100m)
        };

        // Act
        var resultado = await CriarSut().CriarCestaAsync(request);

        // Assert — cesta anterior deve ter sido desativada
        cestaExistente.Ativa.Should().BeFalse();
        cestaExistente.DataDesativacao.Should().NotBeNull();
        resultado.RebalanceamentoDisparado.Should().BeTrue();
        resultado.CestaAnteriorDesativada.Should().NotBeNull();
        resultado.CestaAnteriorDesativada!.Nome.Should().Be("Antiga");
        _cestaRepo.Verify(r => r.AtualizarAsync(cestaExistente), Times.AtLeastOnce);
    }

    // ────────────────────────────────────────────────────────────────────────
    // ObterCestaAtivaAsync
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ObterCestaAtivaAsync_DeveLancarCestaNaoEncontradaException_QuandoNaoHaCestaAtiva()
    {
        // Arrange
        _cestaRepo.Setup(r => r.ObterCestaAtivaAsync()).ReturnsAsync((CestaRecomendacao?)null);

        // Act
        var act = async () => await CriarSut().ObterCestaAtivaAsync();

        // Assert
        await act.Should().ThrowAsync<CestaNaoEncontradaException>();
    }

    [Fact]
    public async Task ObterCestaAtivaAsync_DeveRetornarCestaAtiva_QuandoExistir()
    {
        // Arrange
        var cesta = new CestaRecomendacao("Top Five Feb");
        cesta.ItensCesta.Add(new ItemCesta(1, "PETR4", 30));
        _cestaRepo.Setup(r => r.ObterCestaAtivaAsync()).ReturnsAsync(cesta);

        // Act
        var resultado = await CriarSut().ObterCestaAtivaAsync();

        // Assert
        resultado.Nome.Should().Be("Top Five Feb");
        resultado.Ativa.Should().BeTrue();
        resultado.Itens.Should().HaveCount(1);
    }
}
