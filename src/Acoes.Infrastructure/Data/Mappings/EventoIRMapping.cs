using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class EventoIRMapping : IEntityTypeConfiguration<EventoIR>
{
    public void Configure(EntityTypeBuilder<EventoIR> builder)
    {
        builder.ToTable("EventosIR");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Tipo)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.ValorBase)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.ValorIR)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.PublicadoKafka)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.DataEvento)
            .IsRequired();

        
        builder.HasOne(e => e.Cliente)
            .WithMany(c => c.EventosIR)
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
