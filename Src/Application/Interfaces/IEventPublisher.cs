using FileManager.Domain.Events;

namespace FileManager.Application.Interfaces;

/// <summary>
/// Domain event handler abstraction for handling domain events raised by the SDK.
/// Users implement this interface to handle file lifecycle events (upload, validate, scan, reject, delete).
/// </summary>
/// <remarks>
/// This interface allows users to respond to domain events in their own way:
/// - Publish events to a message broker (RabbitMQ, Azure Service Bus, AWS SQS, Kafka, etc.)
/// - Trigger file validation or virus scanning workflows
/// - Send notifications to users
/// - Update analytics or audit logs
/// - Execute custom business logic
///
/// The SDK calls this interface AFTER successfully saving changes to the database,
/// ensuring consistency between the database state and event handling.
///
/// IMPORTANT: Implementations should handle errors gracefully. If event handling fails,
/// the file operation has already been committed to the database and cannot be rolled back.
/// Consider implementing:
/// - Retry logic for transient failures
/// - Dead letter queues for failed events
/// - Idempotency to handle duplicate event processing
/// - Logging for debugging and monitoring
/// </remarks>
/// <example>
/// <code>
/// // Example: Publish to RabbitMQ
/// public class RabbitMQEventPublisher : IEventPublisher
/// {
///     private readonly IConnection _connection;
///     private readonly IModel _channel;
///
///     public async Task PublishDomainEventsAsync(
///         IEnumerable&lt;DomainEvent&gt; events,
///         CancellationToken cancellationToken = default)
///     {
///         foreach (var domainEvent in events)
///         {
///             var routingKey = $"file.{domainEvent.GetType().Name.ToLowerInvariant()}";
///             var json = JsonSerializer.Serialize(domainEvent);
///             var body = Encoding.UTF8.GetBytes(json);
///
///             _channel.BasicPublish(
///                 exchange: "filemanager.events",
///                 routingKey: routingKey,
///                 body: body);
///         }
///
///         await Task.CompletedTask;
///     }
/// }
///
/// // Example: Trigger validation workflow
/// public class ValidationEventPublisher : IEventPublisher
/// {
///     private readonly IFileService _fileService;
///
///     public async Task PublishDomainEventsAsync(
///         IEnumerable&lt;DomainEvent&gt; events,
///         CancellationToken cancellationToken = default)
///     {
///         foreach (var domainEvent in events)
///         {
///             if (domainEvent is FileUploadedEvent uploadedEvent)
///             {
///                 // Trigger file validation
///                 await _fileService.ValidateFileAsync(
///                     uploadedEvent.FileId,
///                     cancellationToken);
///             }
///         }
///     }
/// }
/// </code>
/// </example>
public interface IEventPublisher
{
    /// <summary>
    /// Handles domain events raised by the SDK after file operations.
    /// </summary>
    /// <param name="events">The collection of domain events to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous event handling operation.</returns>
    /// <remarks>
    /// This method is called by FileService after successfully committing changes to the database.
    /// Events are provided in the order they were raised by the aggregate.
    ///
    /// Common domain events:
    /// - FileUploadedEvent: Raised when a file is uploaded (status: Pending)
    /// - FileValidatedEvent: Raised when a file passes validation (status: Uploaded)
    /// - FileScannedEvent: Raised when a file passes virus scanning (status: Available)
    /// - FileRejectedEvent: Raised when a file is rejected (status: Rejected)
    /// - FileDeletedEvent: Raised when a file is deleted
    ///
    /// Implementations should:
    /// - Handle events in order to maintain proper sequencing
    /// - Implement error handling and retry logic
    /// - Consider idempotency for duplicate event processing
    /// - Log events for debugging and audit purposes
    /// - Execute quickly or use background processing for time-consuming operations
    /// </remarks>
    /// <example>
    /// <code>
    /// // Called by FileService after upload
    /// await _unitOfWork.SaveChangesAsync(cancellationToken);
    ///
    /// if (_eventPublisher != null)
    /// {
    ///     await _eventPublisher.PublishDomainEventsAsync(
    ///         fileMetadata.DomainEvents,
    ///         cancellationToken);
    ///
    ///     fileMetadata.ClearDomainEvents();
    /// }
    /// </code>
    /// </example>
    Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default);
}