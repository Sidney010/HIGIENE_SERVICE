using System.Text;
using Agenda.Domain.Seguranca;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Agenda.Api.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> opcoes, TimeProvider relogio)
{
    private readonly JwtOptions _o = opcoes.Value;

    /// <summary>Claims: sub, estabelecimento_id, perfil (+ cliente_id / profissional_id quando houver).</summary>
    public (string Token, DateTime ExpiraEmUtc) GerarAccessToken(long usuarioId, long estabelecimentoId, Perfil perfil,
        long? clienteId = null, long? profissionalId = null)
    {
        var expira = relogio.GetUtcNow().UtcDateTime.AddMinutes(_o.AccessTokenMinutes);

        var claims = new Dictionary<string, object>
        {
            ["sub"] = usuarioId.ToString(),
            ["estabelecimento_id"] = estabelecimentoId.ToString(),
            ["perfil"] = perfil.ToString(),
        };
        if (clienteId is not null) claims["cliente_id"] = clienteId.Value.ToString();
        if (profissionalId is not null) claims["profissional_id"] = profissionalId.Value.ToString();

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _o.Issuer,
            Audience = _o.Audience,
            Expires = expira,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Key)), SecurityAlgorithms.HmacSha256),
        };

        return (new JsonWebTokenHandler().CreateToken(descritor), expira);
    }
}
