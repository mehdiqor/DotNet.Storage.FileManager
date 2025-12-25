namespace FileManager.Domain.Events;

/// <summary>
/// Base class for all domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}