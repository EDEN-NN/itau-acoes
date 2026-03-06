using System;
using System.Collections.Generic;
using Acoes.Domain.Enums;

namespace Acoes.Domain.Entities;

public class ContaGrafica
{
    public long Id { get; set; }
    public long? ClienteId { get; set; }
    public string NumeroConta { get; set; } = string.Empty;
    public TipoContaGrafica Tipo { get; set; }
    public DateTime DataCriacao { get; set; }

    public Cliente? Cliente { get; set; }
    public ICollection<Custodia> Custodias { get; set; } = new List<Custodia>();

    protected ContaGrafica() { }

    public ContaGrafica(long? clienteId, string numeroConta, TipoContaGrafica tipo)
    {
        ClienteId = clienteId;
        NumeroConta = numeroConta;
        Tipo = tipo;
        DataCriacao = DateTime.UtcNow;
    }
}
