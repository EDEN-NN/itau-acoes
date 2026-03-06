namespace Acoes.Application.DTOs;

public class CarteiraPosicaoDto
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal CotacaoAtual { get; set; }
    public decimal ValorAtual { get; set; }

    
    public decimal PL { get; set; }

    
    public decimal PLPercent { get; set; }

    
    public decimal PercentualCarteira { get; set; }
}
