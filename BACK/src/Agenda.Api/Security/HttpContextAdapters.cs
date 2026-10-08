using Agenda.Api.Middleware;
using Agenda.Application.Abstractions;
using Agenda.Domain.Common;

namespace Agenda.Api.Security;

/// <summary>O estabelecimento vem do token. Em rotas públicas, do cabeçalho X-Estabelecimento (resolvido por middleware).</summary>
public sealed class HttpContextTenantProvider(IHttpContextAccessor accessor) : ITenantProvider
{
    public long EstabelecimentoId
    {
        get
        {
            var ctx = accessor.HttpContext;
            if (long.TryParse(ctx?.User.FindFirst("estabelecimento_id")?.Value, out var doToken))
                return doToken;
            if (ctx?.Items[TenantResolutionMiddleware.ItemKey] is long resolvido)
                return resolvido;
            throw new DomainException("AUTH_PERMISSAO", "Estabelecimento não identificado.", TipoErro.Proibido);
        }
    }
}

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? User => accessor.HttpContext?.User;

    private long? Numero(string claim) =>
        long.TryParse(User?.FindFirst(claim)?.Value, out var v) ? v : null;

    public long? UsuarioId => Numero("sub");
    public string? Perfil => User?.FindFirst("perfil")?.Value;
    public long? ClienteId => Numero("cliente_id");
    public long? ProfissionalId => Numero("profissional_id");
}
