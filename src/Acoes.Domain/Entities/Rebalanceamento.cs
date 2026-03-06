using System;
using Acoes.Domain.Enums;

namespace Acoes.Domain.Entities;

public class Rebalanceamento
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public TipoRebalanceamento Tipo { get; set; }
    public string TickerVendido { get; set; } = string.Empty;
    public string TickerComprado { get; set; } = string.Empty;
    public decimal ValorVenda { get; set; }
    public DateTime DataRebalanceamento { get; set; }

    public Cliente? Cliente { get; set; }

    protected Rebalanceamento() { }

    public Rebalanceamento(long clienteId, TipoRebalanceamento tipo, string tickerVendido, string tickerComprado, decimal valorVenda)
    {
        ClienteId = clienteId;
        Tipo = tipo;
        TickerVendido = tickerVendido;
        TickerComprado = tickerComprado;
        ValorVenda = valorVenda;
        DataRebalanceamento = DateTime.UtcNow;
    }
}
