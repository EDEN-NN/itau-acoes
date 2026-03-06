using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class RebalanceamentoMapping : IEntityTypeConfiguration<Rebalanceamento>
{
    public void Configure(EntityTypeBuilder<Rebalanceamento> builder)
    {
        builder.ToTable("Rebalanceamentos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Tipo)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.TickerVendido)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.TickerComprado)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.ValorVenda)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.DataRebalanceamento)
            .IsRequired();

        
        builder.HasOne(r => r.Cliente)
            .WithMany(c => c.Rebalanceamentos)
            .HasForeignKey(r => r.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
