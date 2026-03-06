using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class DistribuicaoMapping : IEntityTypeConfiguration<Distribuicao>
{
    public void Configure(EntityTypeBuilder<Distribuicao> builder)
    {
        builder.ToTable("Distribuicoes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Quantidade)
            .IsRequired();

        builder.Property(c => c.PrecoUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.DataDistribuicao)
            .IsRequired();

        
        builder.HasOne(d => d.CustodiaFilhote)
            .WithMany(cu => cu.Distribuicoes)
            .HasForeignKey(d => d.CustodiaFilhoteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
