using Agenda.Application.Abstractions;
using Agenda.Domain.Common;
using Agenda.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Agenda.Integration.Tests;

/// <summary>
/// Sobe um PostgreSQL REAL em contêiner (nunca banco em memória: constraints e concorrência se comportam diferente).
/// Requer Docker em execução.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = NovoContexto(tenantId: 0);
        await db.Database.EnsureCreatedAsync();

        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", "001_agendamento_sem_sobreposicao.sql"));
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AgendaDbContext NovoContexto(long tenantId) =>
        new(new DbContextOptionsBuilder<AgendaDbContext>().UseNpgsql(ConnectionString).Options, new TenantFixo(tenantId));

    private sealed class TenantFixo(long id) : ITenantProvider
    {
        public long EstabelecimentoId => id;
    }
}

public sealed class SemEventos : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> eventos, CancellationToken ct) => Task.CompletedTask;
}
