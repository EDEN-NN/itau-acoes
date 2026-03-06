using System;
using System.Collections.Generic;
using Acoes.Domain.Enums;

namespace Acoes.Domain.Entities;

public class OrdemCompra
{
    public long Id { get; set; }
    public long ContaMasterId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public TipoMercado TipoMercado { get; set; }
    public DateTime DataExecucao { get; set; }

    public ContaGrafica? ContaMaster { get; set; }
    public ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();

    protected OrdemCompra() { }

    public OrdemCompra(long contaMasterId, string ticker, int quantidade, decimal precoUnitario, TipoMercado tipoMercado)
    {
        ContaMasterId = contaMasterId;
        Ticker = ticker;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        TipoMercado = tipoMercado;
        DataExecucao = DateTime.UtcNow;
    }
}
