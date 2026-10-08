using Agenda.Domain.Common;

namespace Agenda.Domain.Agendamentos.Events;

// Os eventos carregam o agregado: o Id só existe depois do SaveChanges, e o dispatcher roda depois dele.
public sealed record AgendamentoCriado(Agendamento Agendamento) : DomainEvent;
public sealed record AgendamentoConfirmado(Agendamento Agendamento) : DomainEvent;
public sealed record AgendamentoCancelado(Agendamento Agendamento, bool MultaAplicada, decimal ValorMulta) : DomainEvent;
public sealed record AgendamentoRemarcado(Agendamento Original) : DomainEvent;
public sealed record AtendimentoConcluido(Agendamento Agendamento) : DomainEvent;
public sealed record NoShowRegistrado(Agendamento Agendamento) : DomainEvent;
