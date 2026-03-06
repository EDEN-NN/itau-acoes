using Acoes.Domain.Entities;
using Acoes.Infrastructure.Data.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Context;

public class AcoesDbContext : DbContext
{
    public AcoesDbContext(DbContextOptions<AcoesDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<ContaGrafica> ContasGraficas { get; set; } = null!;
    public DbSet<Custodia> Custodias { get; set; } = null!;
    public DbSet<CestaRecomendacao> CestasRecomendacao { get; set; } = null!;
    public DbSet<ItemCesta> ItensCesta { get; set; } = null!;
    public DbSet<OrdemCompra> OrdensCompra { get; set; } = null!;
    public DbSet<Distribuicao> Distribuicoes { get; set; } = null!;
    public DbSet<EventoIR> EventosIR { get; set; } = null!;
    public DbSet<Rebalanceamento> Rebalanceamentos { get; set; } = null!;
    public DbSet<Cotacao> Cotacoes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcoesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
