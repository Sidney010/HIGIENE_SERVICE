namespace Agenda.Domain.Agendamentos;

public interface IAgendamentoRepository
{
    Task<Agendamento?> ObterAsync(long id, CancellationToken ct);
    Task AdicionarAsync(Agendamento agendamento, CancellationToken ct);

    /// <summary>Agendamentos que ocupam agenda e tocam o intervalo [deUtc, ateUtc).</summary>
    Task<IReadOnlyList<Agendamento>> ListarOcupandoAgendaDoProfissionalAsync(long profissionalId, DateTime deUtc, DateTime ateUtc, CancellationToken ct);
    Task<IReadOnlyList<Agendamento>> ListarOcupandoAgendaDosProfissionaisAsync(IReadOnlyCollection<long> profissionalIds, DateTime deUtc, DateTime ateUtc, CancellationToken ct);
    Task<IReadOnlyList<Agendamento>> ListarOcupandoAgendaDoClienteAsync(long clienteId, DateTime deUtc, DateTime ateUtc, CancellationToken ct);
}
