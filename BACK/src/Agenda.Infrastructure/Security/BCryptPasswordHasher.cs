using Agenda.Application.Abstractions;

namespace Agenda.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int Custo = 11;

    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, Custo);

    public bool Verificar(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
