using System.Collections.Generic;

namespace Acoes.Application.DTOs;

public class RebalancearResponse
{
    public int TotalClientes { get; set; }
    public List<OperacaoRebalanceamentoDto> VendasRealizadas { get; set; } = new();
    public List<OperacaoRebalanceamentoDto> ComprasRealizadas { get; set; } = new();
    public int EventosIRPublicados { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public class OperacaoRebalanceamentoDto
{
    public long ClienteId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    
    public string Tipo { get; set; } = string.Empty;
}
