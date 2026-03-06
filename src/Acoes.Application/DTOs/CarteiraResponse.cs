using System.Collections.Generic;

namespace Acoes.Application.DTOs;

public class CarteiraResponse
{
    public long ClienteId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;

    public decimal ValorInvestidoTotal { get; set; }

    public decimal ValorAtualTotal { get; set; }

    public decimal PLTotal { get; set; }

    public decimal RentabilidadePercent { get; set; }

    public List<CarteiraPosicaoDto> Posicoes { get; set; } = new();
}
