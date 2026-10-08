using Agenda.Api.Contracts;
using Agenda.Application.Abstractions;
using Agenda.Application.Agendamentos;
using Agenda.Application.Agendamentos.Cancelar;
using Agenda.Application.Agendamentos.Criar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agenda.Api.Controllers;

// Parte MANUAL (rotas implementadas). As demais rotas estão em Generated/AgendamentosController.Stubs.cs.
public partial class AgendamentosController(
    IHandler<CriarAgendamentoCommand, AgendamentoDto> criar,
    IHandler<CancelarAgendamentoCommand, CancelarResultadoDto> cancelar)
    : ApiControllerBase
{
    /// <summary>Cria agendamento (status PENDENTE).</summary>
    [HttpPost("agendamentos")]
    [Authorize(Roles = "Admin,Recepcao,Cliente")]
    [ProducesResponseType<AgendamentoDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAgendamentoRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken ct)
    {
        var comando = new CriarAgendamentoCommand(
            request.ClienteId, request.UnidadeId, request.ProfissionalId, request.Inicio,
            request.ServicoIds, request.Origem, idempotencyKey);

        var dto = await criar.HandleAsync(comando, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = dto.Id }, dto);
    }

    /// <summary>Cancela o agendamento; aplica multa se estiver fora do prazo configurado.</summary>
    [HttpPost("agendamentos/{id:long}/cancelar")]
    [Authorize(Roles = "Admin,Recepcao,Cliente")]
    [ProducesResponseType<CancelarResultadoDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancelar(
        long id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CancelarRequest? request,
        CancellationToken ct)
    {
        var resultado = await cancelar.HandleAsync(new CancelarAgendamentoCommand(id, request?.Motivo), ct);
        return Ok(resultado);
    }
}
