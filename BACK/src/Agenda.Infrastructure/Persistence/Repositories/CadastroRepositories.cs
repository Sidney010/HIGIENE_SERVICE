using Agenda.Application.Abstractions;
using Agenda.Domain.Abstractions;
using Agenda.Domain.Cadastro;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository(AgendaDbContext db) : IClienteRepository
{
    public Task<Cliente?> ObterAsync(long id, CancellationToken ct) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExisteTelefoneAsync(string telefone, CancellationToken ct) =>
        db.Clientes.AnyAsync(c => c.Telefone == telefone, ct);

    public async Task AdicionarAsync(Cliente cliente, CancellationToken ct) =>
        await db.Clientes.AddAsync(cliente, ct);
}

public sealed class UnidadeRepository(AgendaDbContext db) : IUnidadeRepository
{
    public Task<Unidade?> ObterAsync(long id, CancellationToken ct) =>
        db.Unidades.FirstOrDefaultAsync(u => u.Id == id && u.Ativo, ct);
}

public sealed class ProfissionalRepository(AgendaDbContext db) : IProfissionalRepository
{
    // Disponibilidade, bloqueios e serviços são "owned": carregam junto com o agregado.
    public Task<Profissional?> ObterAsync(long id, CancellationToken ct) =>
        db.Profissionais.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Profissional>> ListarAtivosDaUnidadeAsync(long unidadeId, CancellationToken ct) =>
        await db.Profissionais.Where(p => p.UnidadeId == unidadeId && p.Ativo).ToListAsync(ct);
}

public sealed class ServicoRepository(AgendaDbContext db) : IServicoRepository
{
    public async Task<IReadOnlyList<Servico>> ListarPorIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        var lista = ids.ToArray();
        return await db.Servicos.Where(s => lista.Contains(s.Id) && s.Ativo).ToListAsync(ct);
    }
}

public sealed class ConfiguracaoRepository(AgendaDbContext db, ITenantProvider tenant) : IConfiguracaoRepository
{
    public async Task<ConfiguracaoEstabelecimento> ObterAsync(CancellationToken ct) =>
        await db.Configuracoes.FirstOrDefaultAsync(ct)
        ?? ConfiguracaoEstabelecimento.Padrao(tenant.EstabelecimentoId);
}
