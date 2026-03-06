using System;
using System.Collections.Generic;

namespace Acoes.Domain.Entities;

public class Custodia
{
    public long Id { get; set; }
    public long ContaGraficaId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoMedio { get; set; }
    public DateTime DataUltimaAtualizacao { get; set; }

    public ContaGrafica? ContaGrafica { get; set; }
    public ICollection<Distribuicao> Distribuicoes { get; set; } = new List<Distribuicao>();

    protected Custodia() { }

    public Custodia(long contaGraficaId, string ticker)
    {
        ContaGraficaId = contaGraficaId;
        Ticker = ticker;
        Quantidade = 0;
        PrecoMedio = 0m;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    public void AdicionarCompra(int quantidade, decimal precoCompra)
    {
        if (quantidade <= 0) return;

        var valorAntigo = this.Quantidade * this.PrecoMedio;
        var valorNovo = quantidade * precoCompra;

        this.Quantidade += quantidade;
        this.PrecoMedio = (valorAntigo + valorNovo) / this.Quantidade;
        this.DataUltimaAtualizacao = DateTime.UtcNow;
    }

    public void RemoverVenda(int quantidadeVendida)
    {
        if (quantidadeVendida <= 0 || quantidadeVendida > this.Quantidade)
            throw new InvalidOperationException("Quantidade de venda invalida ou saldo insuficiente.");

        
        this.Quantidade -= quantidadeVendida;
        this.DataUltimaAtualizacao = DateTime.UtcNow;
    }
}
