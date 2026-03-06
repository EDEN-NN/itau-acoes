using Acoes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acoes.Infrastructure.Data.Mappings;

public class ClienteMapping : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Cpf)
            .IsRequired()
            .HasMaxLength(11);

        builder.HasIndex(c => c.Cpf).IsUnique();

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.ValorMensal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.DataAdesao)
            .IsRequired();

        
        builder.HasOne(c => c.ContaGrafica)
            .WithOne(cg => cg.Cliente)
            .HasForeignKey<ContaGrafica>(cg => cg.ClienteId)
            .OnDelete(DeleteBehavior.SetNull); 
    }
}
