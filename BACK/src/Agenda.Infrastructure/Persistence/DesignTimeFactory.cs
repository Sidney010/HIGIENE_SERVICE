using Agenda.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Agenda.Infrastructure.Persistence;

/// <summary>Usado só pelo `dotnet ef` (migrations). Não participa da execução da API.</summary>
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AgendaDbContext>
{
    public AgendaDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("AGENDA_CONNECTION")
                 ?? "Host=localhost;Port=5432;Database=agenda;Username=agenda;Password=agenda";
        var options = new DbContextOptionsBuilder<AgendaDbContext>().UseNpgsql(cs).Options;
        return new AgendaDbContext(options, new TenantFixo(0));
    }

    private sealed class TenantFixo(long id) : ITenantProvider
    {
        public long EstabelecimentoId => id;
    }
}
