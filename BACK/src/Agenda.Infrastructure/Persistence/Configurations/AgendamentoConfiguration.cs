using Agenda.Domain.Agendamentos;
using Agenda.Domain.Cadastro;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.Infrastructure.Persistence.Configurations;

public sealed class AgendamentoConfiguration : IEntityTypeConfiguration<Agendamento>
{
    public void Configure(EntityTypeBuilder<Agendamento> b)
    {
        b.ToTable("agendamento");
        b.HasKey(x => x.Id);

        // Datas SEMPRE em UTC (timestamptz). A conversão para o fuso da unidade acontece só na borda.
        b.Property(x => x.Inicio).IsRequired();
        b.Property(x => x.Fim).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Origem).HasMaxLength(20);
        b.Property(x => x.MotivoCancelamento).HasMaxLength(300);
        b.Property(x => x.ValorTotal).HasPrecision(12, 2);

        b.HasOne<Unidade>().WithMany().HasForeignKey(x => x.UnidadeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profissional>().WithMany().HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);

        b.OwnsMany(x => x.Itens, o =>
        {
            o.ToTable("item_agendamento");
            o.WithOwner().HasForeignKey("AgendamentoId");
            o.HasKey(x => x.Id);
            o.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            o.Property(x => x.PrecoCongelado).HasPrecision(12, 2);
        });
        b.Navigation(x => x.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(x => new { x.ProfissionalId, x.Inicio });
        b.HasIndex(x => new { x.ClienteId, x.Inicio });
        b.HasIndex(x => new { x.EstabelecimentoId, x.Inicio });

        // A garantia FINAL contra overbooking é a constraint de exclusão do PostgreSQL:
        // veja Persistence/Sql/001_agendamento_sem_sobreposicao.sql (aplique depois da migration inicial).
    }
}
