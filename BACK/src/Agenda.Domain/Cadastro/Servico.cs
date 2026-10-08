using Agenda.Domain.Common;

namespace Agenda.Domain.Cadastro;

public sealed class Especialidade : TenantEntity
{
    private Especialidade() { }

    public string Nome { get; private set; } = string.Empty;

    public static Especialidade Criar(long estabelecimentoId, string nome) =>
        new() { EstabelecimentoId = estabelecimentoId, Nome = Guard.NaoVazio(nome, "Nome", 80) };
}

public sealed class Servico : TenantEntity
{
    private Servico() { }

    public string Nome { get; private set; } = string.Empty;
    public long EspecialidadeId { get; private set; }
    public int DuracaoMin { get; private set; }
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; } = true;

    public static Servico Criar(long estabelecimentoId, long especialidadeId, string nome, int duracaoMin, decimal preco) => new()
    {
        EstabelecimentoId = estabelecimentoId,
        EspecialidadeId = especialidadeId,
        Nome = Guard.NaoVazio(nome, "Nome", 120),
        DuracaoMin = Guard.Positivo(duracaoMin, "Duração"),
        Preco = Guard.NaoNegativo(preco, "Preço"),
    };

    public void Atualizar(string nome, int duracaoMin, decimal preco)
    {
        Nome = Guard.NaoVazio(nome, "Nome", 120);
        DuracaoMin = Guard.Positivo(duracaoMin, "Duração");
        Preco = Guard.NaoNegativo(preco, "Preço");
    }

    public void Inativar() => Ativo = false;
}
