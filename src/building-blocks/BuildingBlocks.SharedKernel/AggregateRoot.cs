namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Raiz de agregado: a única porta de entrada para alterar o agregado e quem registra os eventos de domínio.
/// A persistência lê <see cref="DomainEvents"/> ao salvar e depois chama <see cref="ClearDomainEvents"/>.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
