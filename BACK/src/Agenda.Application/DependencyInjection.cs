using Agenda.Application.Abstractions;
using Agenda.Application.Agendamentos;
using Agenda.Application.Agendamentos.Cancelar;
using Agenda.Application.Agendamentos.Criar;
using Agenda.Application.Agendamentos.HorariosLivres;
using Agenda.Domain.Agendamentos;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Serviços de domínio (sem estado)
        services.AddSingleton<ValidadorDeAgendamento>();
        services.AddSingleton<CalculadoraDeHorariosLivres>();

        // Casos de uso — ao criar um novo, registre aqui.
        services.AddScoped<IHandler<CriarAgendamentoCommand, AgendamentoDto>, CriarAgendamentoHandler>();
        services.AddScoped<IHandler<CancelarAgendamentoCommand, CancelarResultadoDto>, CancelarAgendamentoHandler>();
        services.AddScoped<IHandler<ConsultarHorariosLivresQuery, HorariosLivresDto>, ConsultarHorariosLivresHandler>();

        services.AddValidatorsFromAssemblyContaining<CriarAgendamentoValidator>();

        // Handlers de eventos de domínio (varredura por IDomainEventHandler<>)
        var assembly = typeof(DependencyInjection).Assembly;
        foreach (var tipo in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var contrato in tipo.GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            {
                services.AddScoped(contrato, tipo);
            }
        }

        return services;
    }
}
