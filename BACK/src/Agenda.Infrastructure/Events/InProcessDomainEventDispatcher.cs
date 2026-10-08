using Agenda.Application.Abstractions;
using Agenda.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Infrastructure.Events;

public sealed class InProcessDomainEventDispatcher(IServiceProvider provedor) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> eventos, CancellationToken ct)
    {
        foreach (var evento in eventos)
        {
            var tipoHandler = typeof(IDomainEventHandler<>).MakeGenericType(evento.GetType());
            var metodo = tipoHandler.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in provedor.GetServices(tipoHandler))
            {
                if (handler is null) continue;
                await (Task)metodo.Invoke(handler, [evento, ct])!;
            }
        }
    }
}
