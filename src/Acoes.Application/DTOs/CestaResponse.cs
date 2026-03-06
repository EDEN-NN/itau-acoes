using System;
using System.Collections.Generic;

namespace Acoes.Application.DTOs;

public class CestaResponse
{
    public long CestaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataDesativacao { get; set; }
    public List<ItemCestaResponse> Itens { get; set; } = new();
    public bool RebalanceamentoDisparado { get; set; }
    public CestaAnteriorDto? CestaAnteriorDesativada { get; set; }
    public string? Mensagem { get; set; }
}

public class ItemCestaResponse
{
    public string Ticker { get; set; } = string.Empty;
    public decimal Percentual { get; set; }
}

public class CestaAnteriorDto
{
    public long CestaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataDesativacao { get; set; }
}
