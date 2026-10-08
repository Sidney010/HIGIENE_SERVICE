using Agenda.Application.Abstractions;
using Agenda.Domain.Abstractions;
using Agenda.Domain.Agendamentos;
using Agenda.Domain.Common;
using FluentValidation;

namespace Agenda.Application.Agendamentos.HorariosLivres;

public sealed record ConsultarHorariosLivresQuery(long UnidadeId, IReadOnlyList<long> ServicoIds, DateOnly Data, long? ProfissionalId);

public sealed class ConsultarHorariosLivresValidator : AbstractValidator<ConsultarHorariosLivresQuery>
{
    public ConsultarHorariosLivresValidator()
    {
        RuleFor(x => x.UnidadeId).GreaterThan(0);
        RuleFor(x => x.ServicoIds).NotEmpty().WithMessage("Informe ao menos um serviço.");
    }
}

public sealed class ConsultarHorariosLivresHandler(
    IValidator<ConsultarHorariosLivresQuery> validator,
    TimeProvider relogio,
    IUnidadeRepository unidades,
    IProfissionalRepository profissionais,
    IServicoRepository servicos,
    IConfiguracaoRepository configuracoes,
    IAgendamentoRepository agendamentos,
    CalculadoraDeHorariosLivres calculadora)
    : IHandler<ConsultarHorariosLivresQuery, HorariosLivresDto>
{
    public async Task<HorariosLivresDto> HandleAsync(ConsultarHorariosLivresQuery q, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(q, ct);

        var unidade = await unidades.ObterAsync(q.UnidadeId, ct) ?? throw DomainException.NaoEncontrado("Unidade");
        var ids = q.ServicoIds.Distinct().ToList();
        var servicosEscolhidos = await servicos.ListarPorIdsAsync(ids, ct);
        if (servicosEscolhidos.Count != ids.Count) throw DomainException.NaoEncontrado("Serviço");

        var candidatos = (await profissionais.ListarAtivosDaUnidadeAsync(unidade.Id, ct))
            .Where(p => q.ProfissionalId is null || p.Id == q.ProfissionalId)
            .Where(p => ids.All(id => p.Atende(id)))
            .ToList();
        if (q.ProfissionalId is not null && candidatos.Count == 0)
            throw new DomainException("AGE_PROFISSIONAL_NAO_HABILITADO", "O profissional não atende todos os serviços escolhidos.");

        var config = await configuracoes.ObterAsync(ct);

        // Janela do dia local convertida para UTC, para buscar os agendamentos existentes.
        var diaInicioUtc = unidade.ParaUtc(q.Data, TimeOnly.MinValue);
        var diaFimUtc = diaInicioUtc.AddDays(1);
        var existentes = await agendamentos.ListarOcupandoAgendaDosProfissionaisAsync(
            candidatos.Select(p => p.Id).ToList(), diaInicioUtc, diaFimUtc, ct);

        var agendas = candidatos
            .Select(p => new AgendaDoProfissional(p, existentes.Where(a => a.ProfissionalId == p.Id).ToList()))
            .ToList();

        var horarios = calculadora.Calcular(unidade, config, servicosEscolhidos, agendas, q.Data, relogio.GetUtcNow().UtcDateTime);

        var duracaoTotal = candidatos.Count == 0
            ? servicosEscolhidos.Sum(s => s.DuracaoMin)
            : servicosEscolhidos.Sum(s => candidatos[0].DuracaoEfetiva(s));

        return new HorariosLivresDto(q.Data, duracaoTotal,
            horarios.Select(h => new HorarioLivreDto(h.ProfissionalId, h.InicioUtc, h.FimUtc)).ToList());
    }
}
