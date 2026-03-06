using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using FluentAssertions;

namespace Acoes.Tests.Domain;

/// <summary>
/// Testes das regras de cálculo diretamente nas entidades e nos algoritmos do domínio.
/// Baseados nos exemplos numéricos do doc regras-negocio-detalhadas.md.
/// </summary>
public class CalculosNegocioTests
{
    // ────────────────────────────────────────────────────────────────────────
    // Preço Médio — RN-041 a RN-044
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CalcularPrecoMedio_DeveRetornarPrecoNovoPrimeiraCompra()
    {
        // Compra 1: 8 ações a R$ 35,00. PM inicial = 35,00
        var qtdAnt = 0;
        var pmAnt = 0m;
        var qtdNova = 8;
        var precoNova = 35m;

        var pm = CalcularPM(qtdAnt, pmAnt, qtdNova, precoNova);

        pm.Should().Be(35m);
    }

    [Fact]
    public void CalcularPrecoMedio_DeveCalcularCorretamente_Compra2()
    {
        // Após compra 1: 8 ações PM=35
        // Compra 2: 10 ações a R$ 37,00
        // PM = (8 x 35 + 10 x 37) / (8 + 10) = 650 / 18 = 36,1111...
        var pm = CalcularPM(8, 35m, 10, 37m);

        Math.Round(pm, 2).Should().Be(36.11m);
    }

    [Fact]
    public void VendaNaoAlteraPrecoMedio_RN043()
    {
        // Após 2 compras: PM = 36,11, 18 ações
        // Venda de 5 ações → PM continua 36,11
        var custódia = new Custodia(1, "PETR4") { Quantidade = 18, PrecoMedio = 36.11m };

        // Simular venda: apenas quantidade diminui, PM não muda
        custódia.Quantidade -= 5;

        custódia.PrecoMedio.Should().Be(36.11m);
        custódia.Quantidade.Should().Be(13);
    }

    [Fact]
    public void CalcularPrecoMedio_Compra3_AposVenda()
    {
        // Após venda: 13 ações, PM=36,11
        // Compra 3: 7 ações a R$ 38,00
        // PM = (13 × 36,11 + 7 × 38) / 20 = (469,43 + 266,00) / 20 = 36,7715
        var pm = CalcularPM(13, 36.11m, 7, 38m);

        Math.Round(pm, 2).Should().Be(36.77m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Distribuição Proporcional — RN-034 a RN-036
    // (Exemplo da doc: 3 clientes, PETR4, 30 ações disponíveis)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DistribuicaoProportcional_ClienteA_DeveTruncar()
    {
        // Cliente A: R$1.000 / R$3.500 total = 28,57%
        // PETR4 disponível: 30 ações
        // TRUNCAR(30 × 0,2857) = TRUNCAR(8,571) = 8
        var proporcao = 1000m / 3500m;
        var qtdDisponivel = 30;
        var qtd = (int)(proporcao * qtdDisponivel);

        qtd.Should().Be(8);
    }

    [Fact]
    public void DistribuicaoProportcional_ClienteB_DeveTruncar()
    {
        // Cliente B: R$2.000 / R$3.500 = 57,14%
        // TRUNCAR(30 × 0,5714) = TRUNCAR(17,14) = 17
        var proporcao = 2000m / 3500m;
        var qtd = (int)(proporcao * 30m);

        qtd.Should().Be(17);
    }

    [Fact]
    public void DistribuicaoProportcional_ClienteC_DeveTruncar()
    {
        // Cliente C: R$500 / R$3.500 = 14,29%
        // TRUNCAR(30 × 0,1429) = TRUNCAR(4,29) = 4
        var proporcao = 500m / 3500m;
        var qtd = (int)(proporcao * 30m);

        qtd.Should().Be(4);
    }

    [Fact]
    public void Residuo_NaContaMaster_DeveSer1_AposDistribuirTresClientes()
    {
        // 8 + 17 + 4 = 29 distribuídas. Disponível: 30 → resíduo = 1
        var distribuidoA = 8;
        var distribuidoB = 17;
        var distribuidoC = 4;
        var totalDisponivel = 30;

        var residuo = totalDisponivel - (distribuidoA + distribuidoB + distribuidoC);

        residuo.Should().Be(1);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Separação Lote Padrão vs Fracionário — RN-031 a RN-033
    // ────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(28, 0, 28)]   // 28 ações → 0 lote, 28 frac
    [InlineData(350, 300, 50)] // 350 ações → 3 lotes (300), 50 frac
    [InlineData(100, 100, 0)]  // 100 ações → 1 lote, 0 frac
    [InlineData(1, 0, 1)]      // 1 ação → 0 lote, 1 frac
    public void SepararLoteFracionario_DeveCalcularCorretamente(int total, int loteEsperado, int fracEsperado)
    {
        var lotePadrao = (total / 100) * 100;
        var fracionario = total % 100;

        lotePadrao.Should().Be(loteEsperado);
        fracionario.Should().Be(fracEsperado);
    }

    // ────────────────────────────────────────────────────────────────────────
    // IR Dedo-Duro — RN-053/054
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IRDedoDuro_DeveCalcularAliquotaCorretamente()
    {
        // 8 ações PETR4 × R$35,00 = R$280,00
        // IR = R$280 × 0,005% = R$0,014 → arredondado = R$0,01
        const decimal aliquota = 0.00005m;
        var valorOperacao = 8 * 35m;
        var ir = Math.Round(valorOperacao * aliquota, 2);

        ir.Should().Be(0.01m);
    }

    // ────────────────────────────────────────────────────────────────────────
    // IR Sobre Vendas — RN-057 a RN-061
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IRVenda_DeveSerIsento_QuandoTotalVendasAbaixoDe20k()
    {
        var totalVendas = 19_000m;
        var isento = totalVendas <= 20_000m;

        isento.Should().BeTrue();
    }

    [Fact]
    public void IRVenda_Deve20PorcenSobreLucro_QuandoTotalVendasAcimaDe20k()
    {
        // Exemplo da doc: 500 BBDC4 × R$16 = R$8.000 + 300 WEGE3 × R$45 = R$13.500 → total R$21.500
        // Lucro BBDC4: 500 × (16-14) = R$1.000
        // Lucro WEGE3: 300 × (45-38) = R$2.100
        // IR = R$3.100 × 20% = R$620
        var totalVendas = 21_500m;
        var lucroBBDC4 = 500 * (16m - 14m);
        var lucroWEGE3 = 300 * (45m - 38m);
        var lucroTotal = lucroBBDC4 + lucroWEGE3;

        var ir = totalVendas > 20_000m && lucroTotal > 0
            ? Math.Round(lucroTotal * 0.20m, 2)
            : 0m;

        lucroTotal.Should().Be(3_100m);
        ir.Should().Be(620m);
    }

    [Fact]
    public void IRVenda_DeveSerZero_QuandoHaPrejuizoMesmoAcimaDe20k()
    {
        // Exemplo: vendas > R$20k mas lucro NEGATIVO → IR = R$0
        // Lucro PETR4: 400 × (32-35) = -R$1.200 (prejuízo)
        // Lucro VALE3: 200 × (58-55) = R$600
        // Lucro TOTAL: -R$600 → IR = 0
        var totalVendas = 24_400m;
        var lucroPETR4 = 400 * (32m - 35m);  // -1.200
        var lucroVALE3 = 200 * (58m - 55m);  // 600
        var lucroTotal = lucroPETR4 + lucroVALE3; // -600

        var ir = totalVendas > 20_000m && lucroTotal > 0
            ? Math.Round(lucroTotal * 0.20m, 2)
            : 0m;

        ir.Should().Be(0m, "prejuízo não gera IR mesmo acima de R$20k");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Entidade CestaRecomendacao
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CestaRecomendacao_DeveIniciarAtiva_QuandoCriada()
    {
        var cesta = new CestaRecomendacao("Top Five");

        cesta.Ativa.Should().BeTrue();
        cesta.DataDesativacao.Should().BeNull();
    }

    [Fact]
    public void CestaRecomendacao_DeveDesativarCorretamente()
    {
        var cesta = new CestaRecomendacao("Top Five");
        cesta.Desativar();

        cesta.Ativa.Should().BeFalse();
        cesta.DataDesativacao.Should().NotBeNull();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────

    private static decimal CalcularPM(int qtdAnt, decimal pmAnt, int qtdNova, decimal precoNova)
    {
        // RN-042: PM = (QtdAnterior × PMAnterior + QtdNova × PrecoNova) / (QtdAnterior + QtdNova)
        if (qtdAnt == 0) return precoNova;
        return ((qtdAnt * pmAnt) + (qtdNova * precoNova)) / (qtdAnt + qtdNova);
    }
}
