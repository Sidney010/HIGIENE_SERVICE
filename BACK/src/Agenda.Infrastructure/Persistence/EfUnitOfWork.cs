using Agenda.Application.Abstractions;
using Agenda.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Agenda.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AgendaDbContext db, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    private const string ExclusionViolation = "23P01";
    private const string UniqueViolation = "23505";

    public async Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken ct)
    {
        var estrategia = db.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            // READ COMMITTED + constraint de exclusão no banco: simples e correto para este caso.
            await using var transacao = await db.Database.BeginTransactionAsync(ct);
            var resultado = await operacao();
            await SalvarAsync(ct);
            await transacao.CommitAsync(ct);
            return resultado;
        });
    }

    public async Task SalvarAsync(CancellationToken ct)
    {
        // Coleta os eventos ANTES de salvar (os agregados ainda estão rastreados) e despacha DEPOIS (ids já existem).
        var agregados = db.ChangeTracker.Entries<AggregateRoot>().Select(e => e.Entity).ToList();
        var eventos = agregados.SelectMany(a => a.DomainEvents).ToList();

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: ExclusionViolation })
        {
            // Perdeu a corrida: outro agendamento ocupou o horário entre a validação e o commit.
            throw new DomainException("AGE_HORARIO_OCUPADO", "O profissional já possui atendimento neste horário.", TipoErro.Conflito);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation } pg)
        {
            throw new DomainException("CONFLITO_DUPLICIDADE", $"Registro duplicado ({pg.ConstraintName}).", TipoErro.Conflito);
        }

        agregados.ForEach(a => a.ClearDomainEvents());
        // Evolução (F2): gravar os eventos numa tabela outbox na MESMA transação e publicar por um worker.
        await dispatcher.DispatchAsync(eventos, ct);
    }
}
