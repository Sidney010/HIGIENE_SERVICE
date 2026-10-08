using Agenda.Application.Abstractions;
using Agenda.Domain.Abstractions;
using Agenda.Domain.Agendamentos;
using Agenda.Infrastructure.Events;
using Agenda.Infrastructure.Persistence;
using Agenda.Infrastructure.Persistence.Repositories;
using Agenda.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Agenda")
            ?? throw new InvalidOperationException("ConnectionStrings:Agenda não configurada.");

        services.AddDbContext<AgendaDbContext>(o => o.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IUnidadeRepository, UnidadeRepository>();
        services.AddScoped<IProfissionalRepository, ProfissionalRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IConfiguracaoRepository, ConfiguracaoRepository>();

        return services;
    }
}
