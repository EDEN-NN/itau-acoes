using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class OrdemCompraMapping : IEntityTypeConfiguration<OrdemCompra>
{
    public void Configure(EntityTypeBuilder<OrdemCompra> builder)
    {
        builder.ToTable("OrdensCompra");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Quantidade)
            .IsRequired();

        builder.Property(c => c.PrecoUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.TipoMercado)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.DataExecucao)
            .IsRequired();

        
        builder.HasOne(o => o.ContaMaster)
            .WithMany()
            .HasForeignKey(o => o.ContaMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        
        builder.HasMany(o => o.Distribuicoes)
            .WithOne(d => d.OrdemCompra)
            .HasForeignKey(d => d.OrdemCompraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
