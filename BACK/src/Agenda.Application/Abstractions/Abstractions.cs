using Agenda.Domain.Common;

namespace Agenda.Application.Abstractions;

/// <summary>Caso de uso. Sem MediatR de propósito (licença comercial nas versões novas): simples e testável.</summary>
public interface IHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evento, CancellationToken ct);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> eventos, CancellationToken ct);
}

public interface IUnitOfWork
{
    /// <summary>Executa a operação numa transação, salva e despacha os eventos de domínio.</summary>
    Task<T> ExecutarEmTransacaoAsync<T>(Func<Task<T>> operacao, CancellationToken ct);

    Task SalvarAsync(CancellationToken ct);
}

/// <summary>Estabelecimento (tenant) da requisição atual. Vem SEMPRE do token, nunca do corpo.</summary>
public interface ITenantProvider
{
    long EstabelecimentoId { get; }
}

public interface ICurrentUser
{
    long? UsuarioId { get; }
    string? Perfil { get; }
    long? ClienteId { get; }
    long? ProfissionalId { get; }
}

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);
}

public static class Perfis
{
    public const string Admin = "Admin";
    public const string Recepcao = "Recepcao";
    public const string Profissional = "Profissional";
    public const string Cliente = "Cliente";
}
