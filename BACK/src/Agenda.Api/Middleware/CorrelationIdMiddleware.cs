namespace Agenda.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(Header, out var existente) && !string.IsNullOrWhiteSpace(existente)
            ? existente.ToString()
            : Guid.NewGuid().ToString();

        ctx.Response.Headers[Header] = id;
        ctx.Items[Header] = id;

        using (ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Correlation")
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(ctx);
        }
    }
}
