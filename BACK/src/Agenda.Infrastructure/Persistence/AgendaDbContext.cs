using System.Reflection;
using Agenda.Application.Abstractions;
using Agenda.Domain.Agendamentos;
using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;
using Agenda.Domain.Financeiro;
using Agenda.Domain.Seguranca;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Infrastructure.Persistence;

public sealed class AgendaDbContext(DbContextOptions<AgendaDbContext> options, ITenantProvider tenant)
    : DbContext(options)
{
    public DbSet<Estabelecimento> Estabelecimentos => Set<Estabelecimento>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Especialidade> Especialidades => Set<Especialidade>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<ConfiguracaoEstabelecimento> Configuracoes => Set<ConfiguracaoEstabelecimento>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<Comissao> Comissoes => Set<Comissao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Avaliado a cada consulta pelo EF (vira parâmetro SQL), por isso é propriedade do contexto.</summary>
    private long TenantId => tenant.EstabelecimentoId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgendaDbContext).Assembly);

        var metodoFiltro = typeof(AgendaDbContext).GetMethod(nameof(AplicarFiltroDeTenant), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var tipo in modelBuilder.Model.GetEntityTypes()
                     .Where(t => !t.IsOwned() && typeof(TenantEntity).IsAssignableFrom(t.ClrType))
                     .Select(t => t.ClrType)
                     .ToList())
        {
            // 1) Todo dado de tenant só é visível para o próprio tenant (RF-SAAS-01).
            metodoFiltro.MakeGenericMethod(tipo).Invoke(this, [modelBuilder]);

            // 2) Integridade referencial com o estabelecimento.
            modelBuilder.Entity(tipo)
                .HasOne(typeof(Estabelecimento))
                .WithMany()
                .HasForeignKey("EstabelecimentoId")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    private void AplicarFiltroDeTenant<T>(ModelBuilder modelBuilder) where T : TenantEntity =>
        modelBuilder.Entity<T>().HasQueryFilter(e => e.EstabelecimentoId == TenantId);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Rede de segurança: impede gravar dados de outro estabelecimento por engano.
        var atual = tenant.EstabelecimentoId;
        var invasor = ChangeTracker.Entries<TenantEntity>()
            .FirstOrDefault(e => e.State == EntityState.Added && e.Entity.EstabelecimentoId != atual);
        if (invasor is not null)
            throw new DomainException("AUTH_PERMISSAO", "Tentativa de gravar dado de outro estabelecimento.", TipoErro.Proibido);

        return base.SaveChangesAsync(cancellationToken);
    }
}
