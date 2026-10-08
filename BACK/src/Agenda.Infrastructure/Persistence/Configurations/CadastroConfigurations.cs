using Agenda.Domain.Cadastro;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.Infrastructure.Persistence.Configurations;

public sealed class EstabelecimentoConfiguration : IEntityTypeConfiguration<Estabelecimento>
{
    public void Configure(EntityTypeBuilder<Estabelecimento> b)
    {
        b.ToTable("estabelecimento");
        b.HasKey(x => x.Id);
        b.Property(x => x.RazaoSocial).HasMaxLength(200).IsRequired();
        b.Property(x => x.Cnpj).HasMaxLength(14).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(60).IsRequired();
        b.Property(x => x.Plano).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class UnidadeConfiguration : IEntityTypeConfiguration<Unidade>
{
    public void Configure(EntityTypeBuilder<Unidade> b)
    {
        b.ToTable("unidade");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Endereco).HasMaxLength(250).IsRequired();
        b.Property(x => x.Telefone).HasMaxLength(20);
        b.Property(x => x.FusoHorario).HasMaxLength(60).IsRequired();
        b.HasIndex(x => new { x.EstabelecimentoId, x.Nome }).IsUnique();
    }
}

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("cliente");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Telefone).HasMaxLength(13).IsRequired();
        b.Property(x => x.Email).HasMaxLength(160);
        b.Property(x => x.Observacoes).HasMaxLength(1000);
        // RN-CAD-02: telefone único dentro do estabelecimento.
        b.HasIndex(x => new { x.EstabelecimentoId, x.Telefone }).IsUnique();
    }
}

public sealed class EspecialidadeConfiguration : IEntityTypeConfiguration<Especialidade>
{
    public void Configure(EntityTypeBuilder<Especialidade> b)
    {
        b.ToTable("especialidade");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(80).IsRequired();
        b.HasIndex(x => new { x.EstabelecimentoId, x.Nome }).IsUnique();
    }
}

public sealed class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> b)
    {
        b.ToTable("servico");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Preco).HasPrecision(12, 2);
        b.HasOne<Especialidade>().WithMany().HasForeignKey(x => x.EspecialidadeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProfissionalConfiguration : IEntityTypeConfiguration<Profissional>
{
    public void Configure(EntityTypeBuilder<Profissional> b)
    {
        b.ToTable("profissional");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.ComissaoPadrao).HasPrecision(5, 2);
        b.HasOne<Unidade>().WithMany().HasForeignKey(x => x.UnidadeId).OnDelete(DeleteBehavior.Restrict);

        b.OwnsMany(x => x.ServicosHabilitados, o =>
        {
            o.ToTable("profissional_servico");
            o.WithOwner().HasForeignKey("ProfissionalId");
            o.HasKey(x => x.Id);
            o.Property(x => x.PrecoPersonalizado).HasPrecision(12, 2);
            o.Property(x => x.Comissao).HasPrecision(5, 2);
            o.HasIndex("ProfissionalId", nameof(ProfissionalServico.ServicoId)).IsUnique();
        });
        b.OwnsMany(x => x.Disponibilidades, o =>
        {
            o.ToTable("disponibilidade");
            o.WithOwner().HasForeignKey("ProfissionalId");
            o.HasKey(x => x.Id);
        });
        b.OwnsMany(x => x.Bloqueios, o =>
        {
            o.ToTable("bloqueio");
            o.WithOwner().HasForeignKey("ProfissionalId");
            o.HasKey(x => x.Id);
            o.Property(x => x.Motivo).HasMaxLength(200);
        });

        b.Navigation(x => x.ServicosHabilitados).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Disponibilidades).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Bloqueios).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ConfiguracaoEstabelecimentoConfiguration : IEntityTypeConfiguration<ConfiguracaoEstabelecimento>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoEstabelecimento> b)
    {
        b.ToTable("configuracao_estabelecimento");
        b.HasKey(x => x.Id);
        b.Property(x => x.MultaCancelamentoTardioPercentual).HasPrecision(5, 2);
        b.Property(x => x.LimiteDescontoRecepcaoPercentual).HasPrecision(5, 2);
        b.HasIndex(x => x.EstabelecimentoId).IsUnique();
    }
}
