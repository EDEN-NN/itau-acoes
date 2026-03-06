using System;
using System.Collections.Generic;

namespace Acoes.Application.DTOs;

public class InserirCotacoesResponse
{
    public int TotalInserido { get; set; }
    public DateTime DataPregao { get; set; }
    public List<string> Tickers { get; set; } = new();
    public string Mensagem { get; set; } = string.Empty;
}
