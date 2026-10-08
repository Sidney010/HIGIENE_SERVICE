using Microsoft.AspNetCore.Mvc;

namespace Agenda.Api.Controllers;

/// <summary>
/// Base de todos os controllers. Prefixo único /api/v1 (versionamento no caminho).
/// Os controllers são finos: validam o transporte (HTTP) e delegam para um caso de uso da camada Application.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult NaoImplementado() =>
        StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Status = StatusCodes.Status501NotImplemented,
            Title = "Rota ainda não implementada",
            Detail = "A rota existe no contrato (docs/openapi.yaml), mas o caso de uso ainda não foi implementado.",
        });
}
