using System;
using System.Collections.Generic;

namespace Acoes.Domain.Entities;

public class CestaRecomendacao
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataDesativacao { get; set; }

    public ICollection<ItemCesta> ItensCesta { get; set; } = new List<ItemCesta>();

    protected CestaRecomendacao() { }

    public CestaRecomendacao(string nome)
    {
        Nome = nome;
        Ativa = true;
        DataCriacao = DateTime.UtcNow;
    }

    public void Desativar()
    {
        Ativa = false;
        DataDesativacao = DateTime.UtcNow;
    }
}
