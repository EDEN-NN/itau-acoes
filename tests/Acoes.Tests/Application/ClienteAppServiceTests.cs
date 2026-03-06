using Acoes.Application.DTOs;
using Acoes.Application.Exceptions;
using Acoes.Application.Services;
using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using Acoes.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace Acoes.Tests.Application;

public class ClienteAppServiceTests
{
    // ── Fakes e sut ──────────────────────────────────────────────────────────

    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IContaGraficaRepository> _contaGraficaRepo = new();
    private readonly Mock<ICustodiaRepository> _custodiaRepo = new();
    private readonly Mock<ICestaRecomendacaoRepository> _cestaRepo = new();
    private readonly Mock<ICotacaoRepository> _cotacaoRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ClienteAppService CriarSut() => new(
        _clienteRepo.Object,
        _contaGraficaRepo.Object,
        _custodiaRepo.Object,
        _cestaRepo.Object,
        _cotacaoRepo.Object,
        _unitOfWork.Object);

    // ────────────────────────────────────────────────────────────────────────
    // AderirAsync
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AderirAsync_DeveRetornarAdesaoResponse_QuandoDadosValidos()
    {
        // Arrange
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _cestaRepo.Setup(r => r.ObterCestaAtivaAsync()).ReturnsAsync((CestaRecomendacao?)null);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(true);

        var request = new AdesaoRequest
        {
            Nome = "João Silva",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m
        };

        // Act
        var resultado = await CriarSut().AderirAsync(request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.NumeroConta.Should().StartWith("CTA-");
        resultado.Mensagem.Should().Contain("Adesão realizada");
        _clienteRepo.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
        _contaGraficaRepo.Verify(r => r.AdicionarAsync(It.IsAny<ContaGrafica>()), Times.Once);
    }

    [Fact]
    public async Task AderirAsync_DeveLancarClienteCpfDuplicadoException_QuandoCpfJaCadastrado()
    {
        // Arrange
        var clienteExistente = new Cliente("Outro", "12345678901", "outro@email.com", 500m);
        _clienteRepo.Setup(r => r.ObterPorCpfAsync("12345678901")).ReturnsAsync(clienteExistente);

        var request = new AdesaoRequest
        {
            Nome = "João",
            Cpf = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 500m
        };

        // Act
        var act = async () => await CriarSut().AderirAsync(request);

        // Assert
        await act.Should().ThrowAsync<ClienteCpfDuplicadoException>()
            .WithMessage("*12345678901*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(99.99)]
    public async Task AderirAsync_DeveLancarValorMensalInvalidoException_QuandoValorAbaixoDoMinimo(decimal valorMensal)
    {
        // Arrange
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);

        var request = new AdesaoRequest
        {
            Nome = "Maria",
            Cpf = "98765432100",
            Email = "maria@email.com",
            ValorMensal = valorMensal
        };

        // Act
        var act = async () => await CriarSut().AderirAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValorMensalInvalidoException>();
    }

    [Fact]
    public async Task AderirAsync_DeveCriarCustodiaParaCadaAtivoACestaAtiva()
    {
        // Arrange
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(true);

        var cesta = new CestaRecomendacao("Top 5");
        cesta.ItensCesta.Add(new ItemCesta(1, "PETR4", 30));
        cesta.ItensCesta.Add(new ItemCesta(1, "VALE3", 25));
        cesta.ItensCesta.Add(new ItemCesta(1, "ITUB4", 20));
        cesta.ItensCesta.Add(new ItemCesta(1, "BBDC4", 15));
        cesta.ItensCesta.Add(new ItemCesta(1, "WEGE3", 10));

        _cestaRepo.Setup(r => r.ObterCestaAtivaAsync()).ReturnsAsync(cesta);

        var request = new AdesaoRequest
        {
            Nome = "Carlos",
            Cpf = "11122233344",
            Email = "carlos@email.com",
            ValorMensal = 200m
        };

        // Act
        await CriarSut().AderirAsync(request);

        // Assert — 1 custódia criada por ativo da cesta (5 no total)
        _custodiaRepo.Verify(r => r.AdicionarAsync(It.IsAny<Custodia>()), Times.Exactly(5));
    }

    // ────────────────────────────────────────────────────────────────────────
    // SairDoProdutoAsync
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SairDoProdutoAsync_DeveMarcaClienteComoInativo()
    {
        // Arrange
        var cliente = new Cliente("Ana", "55566677788", "ana@test.com", 300m);
        _clienteRepo.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(true);

        // Act
        await CriarSut().SairDoProdutoAsync(1);

        // Assert
        cliente.Ativo.Should().BeFalse();
        _clienteRepo.Verify(r => r.AtualizarAsync(cliente), Times.Once);
    }

    [Fact]
    public async Task SairDoProdutoAsync_DeveLancarClienteJaInativoException_QuandoClienteJaInativo()
    {
        // Arrange
        var cliente = new Cliente("Pedro", "99988877766", "pedro@test.com", 500m);
        cliente.Ativo = false; // já inativo

        _clienteRepo.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

        // Act
        var act = async () => await CriarSut().SairDoProdutoAsync(1);

        // Assert
        await act.Should().ThrowAsync<ClienteJaInativoException>();
    }

    [Fact]
    public async Task SairDoProdutoAsync_DeveLancarClienteNaoEncontradoException_QuandoClienteNaoExiste()
    {
        // Arrange
        _clienteRepo.Setup(r => r.ObterPorIdAsync(99)).ReturnsAsync((Cliente?)null);

        // Act
        var act = async () => await CriarSut().SairDoProdutoAsync(99);

        // Assert
        await act.Should().ThrowAsync<ClienteNaoEncontradoException>();
    }

    // ────────────────────────────────────────────────────────────────────────
    // AlterarValorMensalAsync
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AlterarValorMensalAsync_DeveAtualizarClienteComNovoValor()
    {
        // Arrange
        var cliente = new Cliente("Ana", "55566677788", "ana@test.com", 300m);
        _clienteRepo.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(true);

        // Act
        await CriarSut().AlterarValorMensalAsync(1, 600m);

        // Assert
        cliente.ValorMensal.Should().Be(600m);
        _clienteRepo.Verify(r => r.AtualizarAsync(cliente), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task AlterarValorMensalAsync_DeveLancarValorMensalInvalidoException_QuandoValorInvalido(decimal novoValor)
    {
        // Arrange
        var cliente = new Cliente("Teste", "11111111111", "t@t.com", 200m);
        _clienteRepo.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);

        // Act
        var act = async () => await CriarSut().AlterarValorMensalAsync(1, novoValor);

        // Assert
        await act.Should().ThrowAsync<ValorMensalInvalidoException>();
    }
}
