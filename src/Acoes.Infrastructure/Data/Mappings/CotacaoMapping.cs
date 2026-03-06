using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class CotacaoMapping : IEntityTypeConfiguration<Cotacao>
{
    public void Configure(EntityTypeBuilder<Cotacao> builder)
    {
        builder.ToTable("Cotacoes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        
        builder.Property(c => c.DataPregao)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(c => c.PrecoAbertura)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoFechamento)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoMaximo)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoMinimo)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        
        builder.HasIndex(c => new { c.Ticker, c.DataPregao }).IsUnique();
    }
}
