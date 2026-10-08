using Agenda.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Api.Middleware;

/// <summary>Rotas públicas: resolve o slug de X-Estabelecimento para o id do tenant (usuário autenticado usa o token).</summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string ItemKey = "tenant_id";
    public const string Header = "X-Estabelecimento";

    public async Task InvokeAsync(HttpContext ctx, AgendaDbContext db)
    {
        var autenticado = ctx.User.Identity?.IsAuthenticated == true;

        if (!autenticado && ctx.Request.Headers.TryGetValue(Header, out var slug) && !string.IsNullOrWhiteSpace(slug))
        {
            var normalizado = slug.ToString().Trim().ToLowerInvariant();
            var id = await db.Estabelecimentos
                .Where(e => e.Slug == normalizado && e.Ativo)
                .Select(e => (long?)e.Id)
                .FirstOrDefaultAsync(ctx.RequestAborted);

            if (id is not null) ctx.Items[ItemKey] = id.Value;
        }

        await next(ctx);
    }
}
