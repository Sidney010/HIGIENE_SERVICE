using Agenda.Domain.Agendamentos;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Infrastructure.Persistence.Repositories;

public sealed class AgendamentoRepository(AgendaDbContext db) : IAgendamentoRepository
{
    private static readonly StatusAgendamento[] Ocupam = Agendamento.StatusQueOcupamAgenda;

    public Task<Agendamento?> ObterAsync(long id, CancellationToken ct) =>
        db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AdicionarAsync(Agendamento agendamento, CancellationToken ct) =>
        await db.Agendamentos.AddAsync(agendamento, ct);

    public async Task<IReadOnlyList<Agendamento>> ListarOcupandoAgendaDoProfissionalAsync(
        long profissionalId, DateTime deUtc, DateTime ateUtc, CancellationToken ct) =>
        await db.Agendamentos
            .Where(a => a.ProfissionalId == profissionalId && Ocupam.Contains(a.Status) && a.Inicio < ateUtc && a.Fim > deUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Agendamento>> ListarOcupandoAgendaDosProfissionaisAsync(
        IReadOnlyCollection<long> profissionalIds, DateTime deUtc, DateTime ateUtc, CancellationToken ct)
    {
        var ids = profissionalIds.ToArray();
        return await db.Agendamentos
            .Where(a => ids.Contains(a.ProfissionalId) && Ocupam.Contains(a.Status) && a.Inicio < ateUtc && a.Fim > deUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Agendamento>> ListarOcupandoAgendaDoClienteAsync(
        long clienteId, DateTime deUtc, DateTime ateUtc, CancellationToken ct) =>
        await db.Agendamentos
            .Where(a => a.ClienteId == clienteId && Ocupam.Contains(a.Status) && a.Inicio < ateUtc && a.Fim > deUtc)
            .ToListAsync(ct);
}
