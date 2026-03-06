using System;

namespace Acoes.Domain.Entities;

public class Distribuicao
{
    public long Id { get; set; }
    public long OrdemCompraId { get; set; }
    public long CustodiaFilhoteId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime DataDistribuicao { get; set; }

    public OrdemCompra? OrdemCompra { get; set; }
    public Custodia? CustodiaFilhote { get; set; }

    protected Distribuicao() { }

    public Distribuicao(long ordemCompraId, long custodiaFilhoteId, string ticker, int quantidade, decimal precoUnitario)
    {
        OrdemCompraId = ordemCompraId;
        CustodiaFilhoteId = custodiaFilhoteId;
        Ticker = ticker;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        DataDistribuicao = DateTime.UtcNow;
    }
}
