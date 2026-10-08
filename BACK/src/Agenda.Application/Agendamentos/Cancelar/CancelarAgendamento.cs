using Agenda.Application.Abstractions;
using Agenda.Domain.Abstractions;
using Agenda.Domain.Agendamentos;
using Agenda.Domain.Common;

namespace Agenda.Application.Agendamentos.Cancelar;

public sealed record CancelarAgendamentoCommand(long AgendamentoId, string? Motivo);

public sealed class CancelarAgendamentoHandler(
    IUnitOfWork uow,
    ICurrentUser usuario,
    TimeProvider relogio,
    IAgendamentoRepository agendamentos,
    IConfiguracaoRepository configuracoes)
    : IHandler<CancelarAgendamentoCommand, CancelarResultadoDto>
{
    public async Task<CancelarResultadoDto> HandleAsync(CancelarAgendamentoCommand cmd, CancellationToken ct)
    {
        return await uow.ExecutarEmTransacaoAsync(async () =>
        {
            var agendamento = await agendamentos.ObterAsync(cmd.AgendamentoId, ct)
                ?? throw DomainException.NaoEncontrado("Agendamento");

            // Cliente só cancela o que é dele; devolvemos 404 para não revelar a existência.
            if (usuario.Perfil == Perfis.Cliente && agendamento.ClienteId != usuario.ClienteId)
                throw DomainException.NaoEncontrado("Agendamento");

            var config = await configuracoes.ObterAsync(ct);
            var resultado = agendamento.Cancelar(cmd.Motivo, relogio.GetUtcNow().UtcDateTime,
                config.PrazoCancelamentoHoras, config.MultaCancelamentoTardioPercentual);

            // TODO: se resultado.MultaAplicada, registrar a cobrança no Financeiro (handler do evento AgendamentoCancelado).
            return new CancelarResultadoDto(agendamento.Id, agendamento.Status.ToString(),
                resultado.MultaAplicada, resultado.ValorMulta);
        }, ct);
    }
}
