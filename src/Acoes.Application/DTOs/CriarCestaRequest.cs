using System.Collections.Generic;

namespace Acoes.Application.DTOs;

public class CriarCestaRequest
{
    public string Nome { get; set; } = string.Empty;
    public List<ItemCestaRequest> Itens { get; set; } = new();
}

public class ItemCestaRequest
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }
}
