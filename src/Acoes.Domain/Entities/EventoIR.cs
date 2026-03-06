using System;
using Acoes.Domain.Enums;

namespace Acoes.Domain.Entities;

public class EventoIR
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public TipoEventoIR Tipo { get; set; }
    public decimal ValorBase { get; set; }
    public decimal ValorIR { get; set; }
    public bool PublicadoKafka { get; set; }
    public DateTime DataEvento { get; set; }

    public Cliente? Cliente { get; set; }

    protected EventoIR() { }

    public EventoIR(long clienteId, TipoEventoIR tipo, decimal valorBase, decimal valorIR)
    {
        ClienteId = clienteId;
        Tipo = tipo;
        ValorBase = valorBase;
        ValorIR = valorIR;
        PublicadoKafka = false;
        DataEvento = DateTime.UtcNow;
    }

    public void MarcarComoPublicado()
    {
        PublicadoKafka = true;
    }
}
