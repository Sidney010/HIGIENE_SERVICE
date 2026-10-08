namespace Agenda.Domain.Common;

public abstract class Entity
{
    public long Id { get; protected set; }
}

/// <summary>Entidade que pertence a um estabelecimento (tenant). Recebe filtro global no EF Core.</summary>
public abstract class TenantEntity : Entity
{
    public long EstabelecimentoId { get; protected set; }

    protected TenantEntity() { }

    protected TenantEntity(long estabelecimentoId) => EstabelecimentoId = estabelecimentoId;
}

public interface IDomainEvent
{
    DateTime OcorreuEmUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OcorreuEmUtc { get; } = DateTime.UtcNow;
}

public abstract class AggregateRoot : TenantEntity
{
    private readonly List<IDomainEvent> _eventos = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _eventos;

    protected AggregateRoot() { }

    protected AggregateRoot(long estabelecimentoId) : base(estabelecimentoId) { }

    protected void Raise(IDomainEvent evento) => _eventos.Add(evento);

    public void ClearDomainEvents() => _eventos.Clear();
}
