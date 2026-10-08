using Agenda.Domain.Common;
using FluentValidation;

namespace Agenda.Api.Middleware;

/// <summary>Traduz exceções de domínio/validação para o padrão de erro da API (RFC 7807 + campo "codigo").</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (DomainException ex)
        {
            await Escrever(ctx, (int)ex.Tipo, ex.Codigo, ex.Message, null);
        }
        catch (ValidationException ex)
        {
            var erros = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await Escrever(ctx, StatusCodes.Status400BadRequest, "VAL_INVALIDO", "Um ou mais campos são inválidos.", erros);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            ctx.Response.StatusCode = 499; // cliente desistiu
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", ctx.Request.Method, ctx.Request.Path);
            // Nunca expor stack trace ou mensagem interna.
            await Escrever(ctx, StatusCodes.Status500InternalServerError, "ERRO_INTERNO", "Ocorreu um erro inesperado.", null);
        }
    }

    private static string Titulo(int status) => status switch
    {
        400 => "Requisição inválida",
        401 => "Não autenticado",
        403 => "Acesso negado",
        404 => "Não encontrado",
        409 => "Conflito",
        422 => "Regra de negócio violada",
        _ => "Erro interno",
    };

    private static Task Escrever(HttpContext ctx, int status, string codigo, string detalhe, IDictionary<string, string[]>? erros)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;

        var extensoes = new Dictionary<string, object?>
        {
            ["codigo"] = codigo,
            ["correlationId"] = ctx.Items[CorrelationIdMiddleware.Header],
        };
        if (erros is not null) extensoes["erros"] = erros;

        return Results.Problem(
            detail: detalhe,
            instance: ctx.Request.Path,
            statusCode: status,
            title: Titulo(status),
            type: $"https://api.exemplo.com/erros/{codigo}",
            extensions: extensoes).ExecuteAsync(ctx);
    }
}
