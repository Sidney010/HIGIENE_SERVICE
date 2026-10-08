namespace Agenda.Domain.Agendamentos;

/// <summary>Os nomes são o formato do contrato da API e do banco (por isso o estilo MAIÚSCULO).</summary>
public enum StatusAgendamento
{
    PENDENTE,
    CONFIRMADO,
    EM_ATENDIMENTO,
    CONCLUIDO,
    CANCELADO,
    NO_SHOW,
    REMARCADO,
}
