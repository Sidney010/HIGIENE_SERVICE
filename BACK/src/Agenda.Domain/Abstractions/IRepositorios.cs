using Agenda.Domain.Cadastro;

namespace Agenda.Domain.Abstractions;

public interface IClienteRepository
{
    Task<Cliente?> ObterAsync(long id, CancellationToken ct);
    Task<bool> ExisteTelefoneAsync(string telefone, CancellationToken ct);
    Task AdicionarAsync(Cliente cliente, CancellationToken ct);
}

public interface IUnidadeRepository
{
    Task<Unidade?> ObterAsync(long id, CancellationToken ct);
}

public interface IProfissionalRepository
{
    Task<Profissional?> ObterAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<Profissional>> ListarAtivosDaUnidadeAsync(long unidadeId, CancellationToken ct);
}

public interface IServicoRepository
{
    Task<IReadOnlyList<Servico>> ListarPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
}

public interface IConfiguracaoRepository
{
    /// <summary>Devolve a configuração do tenant atual ou os valores padrão.</summary>
    Task<ConfiguracaoEstabelecimento> ObterAsync(CancellationToken ct);
}
