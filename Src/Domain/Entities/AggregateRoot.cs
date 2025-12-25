using FileManager.Domain.Events;

namespace FileManager.Domain.Entities;

/// <summary>
/// Base class for aggregate roots in DDD.
/// Aggregates can raise domain events that represent state changes.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate's event collection.
    /// </summary>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events. Called after events have been published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}