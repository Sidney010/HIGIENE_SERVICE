namespace Agenda.Api.Security;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    /// <summary>Em produção use variável de ambiente / secret manager. Mínimo 32 caracteres.</summary>
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
}
