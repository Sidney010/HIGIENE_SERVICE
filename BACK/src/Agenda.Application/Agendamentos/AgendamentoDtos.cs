using Agenda.Domain.Agendamentos;
using Agenda.Domain.Cadastro;

namespace Agenda.Application.Agendamentos;

public sealed record ClienteResumoDto(long Id, string Nome);
public sealed record ProfissionalResumoDto(long Id, string Nome);
public sealed record ItemAgendamentoDto(long ServicoId, string Nome, int DuracaoMin, decimal PrecoCongelado);

public sealed record AgendamentoDto(
    long Id,
    string Status,
    DateTime Inicio,
    DateTime Fim,
    long UnidadeId,
    ClienteResumoDto Cliente,
    ProfissionalResumoDto Profissional,
    IReadOnlyList<ItemAgendamentoDto> Itens,
    decimal ValorTotal,
    string? Origem,
    DateTime CriadoEm)
{
    public static AgendamentoDto De(Agendamento a, Cliente cliente, Profissional profissional) => new(
        a.Id,
        a.Status.ToString(),
        a.Inicio,
        a.Fim,
        a.UnidadeId,
        new ClienteResumoDto(cliente.Id, cliente.Nome),
        new ProfissionalResumoDto(profissional.Id, profissional.Nome),
        a.Itens.Select(i => new ItemAgendamentoDto(i.ServicoId, i.Nome, i.DuracaoMin, i.PrecoCongelado)).ToList(),
        a.ValorTotal,
        a.Origem,
        a.CriadoEmUtc);
}

public sealed record CancelarResultadoDto(long Id, string Status, bool MultaAplicada, decimal ValorMulta);

public sealed record HorarioLivreDto(long ProfissionalId, DateTime Inicio, DateTime Fim);

public sealed record HorariosLivresDto(DateOnly Data, int DuracaoTotalMin, IReadOnlyList<HorarioLivreDto> Horarios);
