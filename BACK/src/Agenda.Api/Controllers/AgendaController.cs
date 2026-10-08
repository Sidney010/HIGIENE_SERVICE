using Agenda.Application.Abstractions;
using Agenda.Application.Agendamentos;
using Agenda.Application.Agendamentos.HorariosLivres;
using Agenda.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agenda.Api.Controllers;

// Parte MANUAL. A rota GET /agenda (visão da recepção) está em Generated/AgendaController.Stubs.cs.
public partial class AgendaController(IHandler<ConsultarHorariosLivresQuery, HorariosLivresDto> horariosLivres)
    : ApiControllerBase
{
    /// <summary>Horários livres para os serviços escolhidos. servicoIds = lista separada por vírgula (ex.: 1,2).</summary>
    [HttpGet("agenda/horarios-livres")]
    [Authorize(Roles = "Admin,Recepcao,Profissional,Cliente")]
    [ProducesResponseType<HorariosLivresDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> HorariosLivres(
        [FromQuery] long unidadeId,
        [FromQuery] string servicoIds,
        [FromQuery] DateOnly data,
        [FromQuery] long? profissionalId,
        CancellationToken ct)
    {
        var ids = new List<long>();
        foreach (var parte in servicoIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!long.TryParse(parte, out var id))
                throw DomainException.Invalido($"servicoIds inválido: '{parte}'.");
            ids.Add(id);
        }

        var resultado = await horariosLivres.HandleAsync(new ConsultarHorariosLivresQuery(unidadeId, ids, data, profissionalId), ct);
        return Ok(resultado);
    }
}
