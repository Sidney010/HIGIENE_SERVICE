using Agenda.Application.Abstractions;
using Agenda.Domain.Agendamentos.Events;
using Microsoft.Extensions.Logging;

namespace Agenda.Application.Agendamentos.EventHandlers;

/// <summary>
/// Exemplo de reação a evento de domínio. Em produção: gravar numa tabela outbox e deixar um worker
/// enviar a confirmação (WhatsApp/SMS/e-mail) com retentativa, sem acoplar ao fluxo de agendamento.
/// </summary>
public sealed class EnfileirarConfirmacaoAoCriarAgendamento(ILogger<EnfileirarConfirmacaoAoCriarAgendamento> logger)
    : IDomainEventHandler<AgendamentoCriado>
{
    public Task HandleAsync(AgendamentoCriado evento, CancellationToken ct)
    {
        logger.LogInformation("Confirmação do agendamento {AgendamentoId} enfileirada (cliente {ClienteId}).",
            evento.Agendamento.Id, evento.Agendamento.ClienteId);
        return Task.CompletedTask;
    }
}
