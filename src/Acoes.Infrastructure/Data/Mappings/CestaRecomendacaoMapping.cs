using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class CestaRecomendacaoMapping : IEntityTypeConfiguration<CestaRecomendacao>
{
    public void Configure(EntityTypeBuilder<CestaRecomendacao> builder)
    {
        builder.ToTable("CestasRecomendacao");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Ativa)
            .IsRequired();

        builder.Property(c => c.DataCriacao)
            .IsRequired();

        
        builder.HasMany(c => c.ItensCesta)
            .WithOne(i => i.Cesta)
            .HasForeignKey(i => i.CestaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
