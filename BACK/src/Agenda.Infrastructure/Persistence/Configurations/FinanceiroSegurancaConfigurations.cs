using Agenda.Domain.Financeiro;
using Agenda.Domain.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.Infrastructure.Persistence.Configurations;

public sealed class ComandaConfiguration : IEntityTypeConfiguration<Comanda>
{
    public void Configure(EntityTypeBuilder<Comanda> b)
    {
        b.ToTable("comanda");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.AgendamentoId).IsUnique(); // uma comanda por agendamento

        b.OwnsMany(x => x.Itens, o =>
        {
            o.ToTable("item_comanda");
            o.WithOwner().HasForeignKey("ComandaId");
            o.HasKey(x => x.Id);
            o.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            o.Property(x => x.Preco).HasPrecision(12, 2);
            o.Property(x => x.ComissaoPercentual).HasPrecision(5, 2);
        });
        b.OwnsMany(x => x.Descontos, o =>
        {
            o.ToTable("desconto_comanda");
            o.WithOwner().HasForeignKey("ComandaId");
            o.HasKey(x => x.Id);
            o.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            o.Property(x => x.Valor).HasPrecision(12, 2);
            o.Property(x => x.ValorCalculado).HasPrecision(12, 2);
            o.Property(x => x.Motivo).HasMaxLength(300).IsRequired();
        });
        b.OwnsMany(x => x.Pagamentos, o =>
        {
            o.ToTable("pagamento");
            o.WithOwner().HasForeignKey("ComandaId");
            o.HasKey(x => x.Id);
            o.Property(x => x.Forma).HasConversion<string>().HasMaxLength(20);
            o.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            o.Property(x => x.Valor).HasPrecision(12, 2);
            o.Property(x => x.MotivoEstorno).HasMaxLength(300);
        });

        b.Navigation(x => x.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Descontos).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Pagamentos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ComissaoConfiguration : IEntityTypeConfiguration<Comissao>
{
    public void Configure(EntityTypeBuilder<Comissao> b)
    {
        b.ToTable("comissao");
        b.HasKey(x => x.Id);
        b.Property(x => x.Valor).HasPrecision(12, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.ProfissionalId, x.GeradaEmUtc });
    }
}

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(160).IsRequired();
        b.Property(x => x.SenhaHash).HasMaxLength(200).IsRequired();
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.EstabelecimentoId, x.Email }).IsUnique();
    }
}
