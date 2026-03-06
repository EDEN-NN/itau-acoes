using System;
using System.Collections.Generic;

namespace Acoes.Domain.Entities;

public class Cliente
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataAdesao { get; set; }

    public ContaGrafica? ContaGrafica { get; set; }
    public ICollection<EventoIR> EventosIR { get; set; } = new List<EventoIR>();
    public ICollection<Rebalanceamento> Rebalanceamentos { get; set; } = new List<Rebalanceamento>();

    protected Cliente() { }

    public Cliente(string nome, string cpf, string email, decimal valorMensal)
    {
        Nome = nome;
        Cpf = cpf;
        Email = email;
        ValorMensal = valorMensal;
        DataAdesao = DateTime.UtcNow;
        Ativo = true;
    }
}
