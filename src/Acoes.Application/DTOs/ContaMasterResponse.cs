using System.Collections.Generic;

namespace Acoes.Application.DTOs;

public class ContaMasterResponse
{
    public long Id { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public List<CustodiaItemDto> Custodia { get; set; } = new();
    public decimal ValorTotalResiduo { get; set; }
}

public class CustodiaItemDto
{
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public decimal ValorAtual { get; set; }
}
