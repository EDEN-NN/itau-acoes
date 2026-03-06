using System;

namespace Acoes.Domain.Entities;

public class ItemCesta
{
    public long Id { get; set; }
    public long CestaId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }

    public CestaRecomendacao? Cesta { get; set; }

    protected ItemCesta() { }

    public ItemCesta(long cestaId, string ticker, decimal percentual)
    {
        CestaId = cestaId;
        Ticker = ticker;
        Percentual = percentual;
    }
}
