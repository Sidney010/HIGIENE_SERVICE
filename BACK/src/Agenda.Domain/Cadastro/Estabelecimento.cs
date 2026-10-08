using Agenda.Domain.Common;

namespace Agenda.Domain.Cadastro;

/// <summary>Raiz do multi-tenant. Não é TenantEntity: é o próprio tenant.</summary>
public sealed class Estabelecimento : Entity
{
    private Estabelecimento() { }

    public string RazaoSocial { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Plano { get; private set; } = "MVP";
    public bool Ativo { get; private set; } = true;

    public static Estabelecimento Criar(string razaoSocial, string cnpj, string slug) => new()
    {
        RazaoSocial = Guard.NaoVazio(razaoSocial, "Razão social"),
        Cnpj = new string(Guard.NaoVazio(cnpj, "CNPJ", 20).Where(char.IsDigit).ToArray()),
        Slug = Guard.NaoVazio(slug, "Slug", 60).ToLowerInvariant(),
    };
}
