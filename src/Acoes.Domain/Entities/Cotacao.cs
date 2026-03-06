using System;

namespace Acoes.Domain.Entities;

public class Cotacao
{
    public long Id { get; set; }
    public DateTime DataPregao { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public decimal PrecoAbertura { get; set; }
    public decimal PrecoFechamento { get; set; }
    public decimal PrecoMaximo { get; set; }
    public decimal PrecoMinimo { get; set; }

    protected Cotacao() { }

    public Cotacao(DateTime dataPregao, string ticker, decimal precoAbertura, decimal precoFechamento, decimal precoMaximo, decimal precoMinimo)
    {
        DataPregao = dataPregao.Date;
        Ticker = ticker;
        PrecoAbertura = precoAbertura;
        PrecoFechamento = precoFechamento;
        PrecoMaximo = precoMaximo;
        PrecoMinimo = precoMinimo;
    }
}
