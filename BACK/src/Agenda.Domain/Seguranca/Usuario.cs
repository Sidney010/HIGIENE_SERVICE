using Agenda.Domain.Common;

namespace Agenda.Domain.Seguranca;

public enum Perfil { Admin, Recepcao, Profissional, Cliente }

public sealed class Usuario : TenantEntity
{
    private Usuario() { }

    public string Email { get; private set; } = string.Empty;
    /// <summary>Hash bcrypt/argon2. Nunca a senha, nunca em log.</summary>
    public string SenhaHash { get; private set; } = string.Empty;
    public Perfil Perfil { get; private set; }
    public bool Ativo { get; private set; } = true;
    public long? ClienteId { get; private set; }
    public long? ProfissionalId { get; private set; }

    public static Usuario Criar(long estabelecimentoId, string email, string senhaHash, Perfil perfil,
        long? clienteId = null, long? profissionalId = null) => new()
    {
        EstabelecimentoId = estabelecimentoId,
        Email = Guard.NaoVazio(email, "E-mail", 160).ToLowerInvariant(),
        SenhaHash = Guard.NaoVazio(senhaHash, "Hash da senha", 200),
        Perfil = perfil,
        ClienteId = clienteId,
        ProfissionalId = profissionalId,
    };

    public void AlterarSenha(string novoHash) => SenhaHash = Guard.NaoVazio(novoHash, "Hash da senha", 200);
    public void AlterarPerfil(Perfil perfil) => Perfil = perfil;
    public void Ativar(bool ativo) => Ativo = ativo;
}
