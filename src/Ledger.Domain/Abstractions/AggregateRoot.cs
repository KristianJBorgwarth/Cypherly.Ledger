// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace Ledger.Domain.Abstractions;

public abstract class AggregateRoot(Guid id) : Entity(id)
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}