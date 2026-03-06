using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class ContaGraficaMapping : IEntityTypeConfiguration<ContaGrafica>
{
    public void Configure(EntityTypeBuilder<ContaGrafica> builder)
    {
        builder.ToTable("ContasGraficas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NumeroConta)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.NumeroConta).IsUnique();

        builder.Property(c => c.Tipo)
            .IsRequired()
            .HasConversion<string>(); 

        builder.Property(c => c.DataCriacao)
            .IsRequired();

        
        builder.HasMany(c => c.Custodias)
            .WithOne(cu => cu.ContaGrafica)
            .HasForeignKey(cu => cu.ContaGraficaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
