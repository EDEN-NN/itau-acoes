using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class CustodiaMapping : IEntityTypeConfiguration<Custodia>
{
    public void Configure(EntityTypeBuilder<Custodia> builder)
    {
        builder.ToTable("Custodias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Quantidade)
            .IsRequired();

        builder.Property(c => c.PrecoMedio)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.DataUltimaAtualizacao)
            .IsRequired();
            
        
        builder.HasIndex(c => new { c.ContaGraficaId, c.Ticker }).IsUnique();
    }
}
