# Event Handling Implementation Guide

## Overview

FileManager SDK uses **domain events** to notify you about file lifecycle changes. When files are uploaded, validated, scanned, rejected, or deleted, the SDK raises domain events that you can handle in your own way.

## How It Works

1. **SDK raises domain events** when file operations occur (upload, validate, scan, reject, delete)
2. **You implement `IEventPublisher`** to handle these events however you want
3. **Your event handler is called** automatically after the database transaction commits
4. **You can**:
   - Publish events to a message broker (RabbitMQ, Azure Service Bus, AWS SQS, Kafka, etc.)
   - Trigger file validation workflows
   - Start virus scanning processes
   - Send notifications to users
   - Update analytics or audit logs
   - Execute any custom business logic

## Quick Start

### 1. Implement IEventPublisher

```csharp
using FileManager.Application.Interfaces;
using FileManager.Domain.Events;

public class MyEventPublisher : IEventPublisher
{
    private readonly ILogger<MyEventPublisher> _logger;

    public MyEventPublisher(ILogger<MyEventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            _logger.LogInformation(
                "Handling event: {EventType} at {Timestamp}",
                domainEvent.GetType().Name,
                domainEvent.OccurredAt);

            // Handle each event type
            switch (domainEvent)
            {
                case FileUploadedEvent uploaded:
                    await HandleFileUploadedAsync(uploaded, cancellationToken);
                    break;

                case FileValidatedEvent validated:
                    await HandleFileValidatedAsync(validated, cancellationToken);
                    break;

                case FileScannedEvent scanned:
                    await HandleFileScannedAsync(scanned, cancellationToken);
                    break;

                case FileRejectedEvent rejected:
                    await HandleFileRejectedAsync(rejected, cancellationToken);
                    break;

                case FileDeletedEvent deleted:
                    await HandleFileDeletedAsync(deleted, cancellationToken);
                    break;
            }
        }
    }

    private async Task HandleFileUploadedAsync(
        FileUploadedEvent @event,
        CancellationToken cancellationToken)
    {
        // Your custom logic here
        _logger.LogInformation("File uploaded: {FileName}", @event.FileName);
    }

    // Implement other handler methods...
}
```

### 2. Register Your Implementation

```csharp
// In Program.cs or Startup.cs
builder.Services.AddFileManager(builder.Configuration);

// Register your event publisher implementation
builder.Services.AddSingleton<IEventPublisher, MyEventPublisher>();
```

### 3. Events Are Published Automatically

The SDK automatically publishes events after each file operation:

```csharp
// When you upload a file
var file = await _fileService.UploadFileAsync(request);
// → FileUploadedEvent is raised and your handler is called

// When object storage webhook triggers validation
await _fileService.ValidateFileAsync(storageKey, actualMetadata);
// → FileValidatedEvent is raised (if validation passes)
// → FileRejectedEvent is raised (if validation fails)

// Note: FileScannedEvent is raised when the domain entity's MarkAsAvailable() method is called
// External virus scanning integration would trigger this event
```

## Domain Events Reference

### FileUploadedEvent

Raised when a file is successfully uploaded to storage.

```csharp
public record FileUploadedEvent(
    Guid FileId,
    string FileName,
    string Path,
    long Size,
    string ContentType,
    string StorageKey,
    StorageProvider Provider,
    DateTime OccurredAt
) : DomainEvent(OccurredAt);
```

**When**: After file upload completes
**Status**: File is now `Pending`
**Common Actions**: Trigger validation workflow, send upload notification

### FileValidatedEvent

Raised when a file passes validation checks.

```csharp
public record FileValidatedEvent(
    Guid FileId,
    DateTime OccurredAt
) : DomainEvent(OccurredAt);
```

**When**: After `ValidateFileAsync` succeeds
**Status**: File is now `Uploaded` (if virus scanning enabled) or `Available` (if virus scanning disabled)
**Common Actions**: Trigger virus scanning (if enabled), send validation success notification

### FileScannedEvent

Raised when a file passes virus scanning.

```csharp
public record FileScannedEvent(
    Guid FileId,
    DateTime OccurredAt
) : DomainEvent(OccurredAt);
```

**When**: When the file status transitions to `Available` (via domain entity's MarkAsAvailable method)
**Status**: File is now `Available`
**Common Actions**: Send availability notification, process the file, allow downloads
**Note**: Currently triggered by domain logic; external virus scanning integration would need to be implemented

### FileRejectedEvent

Raised when a file is rejected (failed validation or virus scan).

```csharp
public record FileRejectedEvent(
    Guid FileId,
    string Reason,
    DateTime OccurredAt
) : DomainEvent(OccurredAt);
```

**When**: When the file is rejected (via ValidateFileAsync when validation fails)
**Status**: File is now `Rejected`
**Common Actions**: Send rejection notification, alert user
**Note**: ValidateFileAsync automatically deletes the file from storage when validation fails

### FileDeletedEvent

Raised when a file is deleted.

```csharp
public record FileDeletedEvent(
    Guid FileId,
    string StorageKey,
    DateTime OccurredAt
) : DomainEvent(OccurredAt);
```

**When**: After `DeleteFileAsync` completes
**Status**: File is marked as deleted
**Common Actions**: Clean up related resources, send deletion notification

## Implementation Examples

### Example 1: Publish to RabbitMQ

```csharp
using RabbitMQ.Client;
using System.Text.Json;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventPublisher> _logger;

    public RabbitMQEventPublisher(
        IConnection connection,
        ILogger<RabbitMQEventPublisher> logger)
    {
        _connection = connection;
        _channel = _connection.CreateModel();
        _logger = logger;

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: "filemanager.events",
            type: ExchangeType.Topic,
            durable: true);
    }

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var routingKey = $"file.{domainEvent.GetType().Name.ToLowerInvariant()}";
            var json = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(
                ((DateTimeOffset)domainEvent.OccurredAt).ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: "filemanager.events",
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published event {EventType} to RabbitMQ with routing key {RoutingKey}",
                domainEvent.GetType().Name,
                routingKey);
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

// Registration
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };
    return factory.CreateConnection();
});
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
```

### Example 2: Trigger Validation Workflow

```csharp
public class ValidationWorkflowEventPublisher : IEventPublisher
{
    private readonly IFileService _fileService;
    private readonly ILogger<ValidationWorkflowEventPublisher> _logger;

    public ValidationWorkflowEventPublisher(
        IFileService fileService,
        ILogger<ValidationWorkflowEventPublisher> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            if (domainEvent is FileUploadedEvent uploadedEvent)
            {
                _logger.LogInformation(
                    "File uploaded, triggering validation: {FileId}",
                    uploadedEvent.FileId);

                // Trigger validation automatically after upload
                await _fileService.ValidateFileAsync(
                    uploadedEvent.FileId,
                    cancellationToken);
            }
        }
    }
}
```

### Example 3: Azure Service Bus

```csharp
using Azure.Messaging.ServiceBus;

public class AzureServiceBusEventPublisher : IEventPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusEventPublisher> _logger;

    public AzureServiceBusEventPublisher(
        ServiceBusClient client,
        ILogger<AzureServiceBusEventPublisher> logger)
    {
        _client = client;
        _sender = _client.CreateSender("filemanager-events");
        _logger = logger;
    }

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ServiceBusMessage>();

        foreach (var domainEvent in events)
        {
            var json = JsonSerializer.Serialize(domainEvent);
            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                Subject = domainEvent.GetType().Name,
                MessageId = Guid.NewGuid().ToString()
            };

            message.ApplicationProperties.Add("EventType", domainEvent.GetType().Name);
            message.ApplicationProperties.Add("FileId", domainEvent.GetType()
                .GetProperty("FileId")?.GetValue(domainEvent)?.ToString() ?? "");

            messages.Add(message);
        }

        if (messages.Count > 0)
        {
            await _sender.SendMessagesAsync(messages, cancellationToken);
            _logger.LogInformation("Published {Count} events to Azure Service Bus", messages.Count);
        }
    }
}

// Registration
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureServiceBus");
    return new ServiceBusClient(connectionString);
});
builder.Services.AddSingleton<IEventPublisher, AzureServiceBusEventPublisher>();
```

### Example 4: Send Email Notifications

```csharp
public class EmailNotificationEventPublisher : IEventPublisher
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationEventPublisher> _logger;

    public EmailNotificationEventPublisher(
        IEmailService emailService,
        ILogger<EmailNotificationEventPublisher> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
                case FileUploadedEvent uploaded:
                    await _emailService.SendAsync(
                        to: "admin@example.com",
                        subject: "File Uploaded",
                        body: $"File {uploaded.FileName} was uploaded successfully.",
                        cancellationToken);
                    break;

                case FileRejectedEvent rejected:
                    await _emailService.SendAsync(
                        to: "admin@example.com",
                        subject: "File Rejected",
                        body: $"File was rejected: {rejected.Reason}",
                        cancellationToken);
                    break;
            }
        }
    }
}
```

## File Validation Workflow

The SDK provides a `ValidateFileAsync` method that you can call from your event handler:

```csharp
public class ValidationEventPublisher : IEventPublisher
{
    private readonly IFileService _fileService;

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            // When a file is uploaded, trigger validation via webhook
            if (domainEvent is FileUploadedEvent uploadedEvent)
            {
                // NOTE: You don't typically call ValidateFileAsync here!
                // Instead, your object storage webhook receives the upload event
                // and calls ValidateFileAsync with the actual metadata.

                // See the webhook example below for the correct pattern.
            }

            // When a file is validated, optionally trigger virus scanning
            if (domainEvent is FileValidatedEvent validatedEvent)
            {
                // If virus scanning is enabled, trigger it here
                // await _virusScanningService.ScanAsync(validatedEvent.FileId);
            }
        }
    }
}
```

The validation workflow:

1. **File is uploaded to object storage**
2. **Object storage triggers webhook** with actual file metadata (size, content type, etc.)
3. **Webhook handler calls `ValidateFileAsync`** with received metadata
4. **SDK checks if metadata is incomplete** (e.g., S3 missing ContentType):
   - If ContentType is null → SDK automatically fetches complete metadata from storage using `GetMetadataAsync`
   - This ensures validation has all required data
5. **Method compares actual metadata with database metadata**:
   - File size matches expected size
   - Content type matches expected type
   - File size is within limits (`MaxFileSizeBytes`)
   - Content type is valid and not empty
   - File name is valid
6. **If validation fails**:
   - **File is DELETED from object storage**
   - File status is set to `Rejected` with error message
   - `FileRejectedEvent` is raised
7. **If validation passes**:
   - If virus scanning is **disabled** → status = `Available` (ready to use)
   - If virus scanning is **enabled** → status = `Uploaded` (awaiting scan)
   - `FileValidatedEvent` is raised

**IMPORTANT**:
- Failed validation automatically removes the file from storage to prevent orphaned files
- SDK automatically handles incomplete webhook data by fetching from storage (S3/SeaweedFS compatibility)

### Webhook Handler Example

```csharp
// Webhook endpoint that receives upload notifications from object storage
[HttpPost("webhooks/file-uploaded")]
public async Task<IActionResult> HandleFileUploadedWebhook([FromBody] StorageWebhookPayload payload)
{
    _logger.LogInformation(
        "Received file upload webhook for storage key: {StorageKey}",
        payload.StorageKey);

    // Create StorageObjectMetadata from webhook payload
    var actualMetadata = new StorageObjectMetadata(
        Key: payload.StorageKey,
        Size: payload.Size,
        ETag: payload.ETag,
        ContentType: payload.ContentType,
        LastModified: payload.UploadedAt
    );

    // Validate the file using storage key and actual metadata from webhook
    // The method will look up the file in database by storage key
    await _fileService.ValidateFileAsync(
        payload.StorageKey,
        actualMetadata,
        HttpContext.RequestAborted);

    return Ok();
}

// Example webhook payload model
public record StorageWebhookPayload(
    string StorageKey,
    long Size,
    string? ContentType,  // Nullable because S3 events don't include this
    string ETag,
    DateTime UploadedAt
);
```

**Important Notes:**

**For AWS S3:** S3 event notifications don't include `ContentType`. The SDK handles this automatically:
- If `ContentType` is `null` in the webhook, the SDK automatically calls `GetMetadataAsync` to fetch complete metadata from S3
- This ensures validation has all required data without extra work in your webhook handler

```csharp
// For S3, just pass null for ContentType - SDK will fetch it automatically
var actualMetadata = new StorageObjectMetadata(
    Key: payload.StorageKey,
    Size: payload.Size,
    ETag: payload.ETag,
    ContentType: null,  // SDK will fetch from S3 automatically
    LastModified: payload.UploadedAt
);

await _fileService.ValidateFileAsync(payload.StorageKey, actualMetadata);
// ✅ SDK detects null ContentType and fetches complete metadata from S3
```

**For MinIO:** MinIO event notifications include all fields including `ContentType`, so no additional fetch is needed.

**For SeaweedFS:** SeaweedFS doesn't have built-in webhook support. You'll need to implement custom webhooks. If your webhook doesn't include `ContentType`, the SDK will automatically fetch it from SeaweedFS storage.

## Best Practices

### 1. Error Handling

Always implement robust error handling in your event publisher:

```csharp
public async Task PublishDomainEventsAsync(
    IEnumerable<DomainEvent> events,
    CancellationToken cancellationToken = default)
{
    foreach (var domainEvent in events)
    {
        try
        {
            await HandleEventAsync(domainEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling event {EventType}: {Message}",
                domainEvent.GetType().Name,
                ex.Message);

            // Consider:
            // - Retry logic
            // - Dead letter queue
            // - Alert notifications
        }
    }
}
```

### 2. Idempotency

Ensure your event handlers are idempotent (can be called multiple times safely):

```csharp
private async Task HandleFileUploadedAsync(
    FileUploadedEvent @event,
    CancellationToken cancellationToken)
{
    // Check if already processed
    if (await _processedEvents.ExistsAsync(@event.FileId, cancellationToken))
    {
        _logger.LogInformation("Event already processed: {FileId}", @event.FileId);
        return;
    }

    // Process event
    await ProcessEventAsync(@event, cancellationToken);

    // Mark as processed
    await _processedEvents.AddAsync(@event.FileId, cancellationToken);
}
```

### 3. Async Processing

For time-consuming operations, use background processing:

```csharp
public class BackgroundEventPublisher : IEventPublisher
{
    private readonly IBackgroundTaskQueue _taskQueue;

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            // Queue for background processing instead of blocking
            _taskQueue.QueueBackgroundWorkItem(async ct =>
            {
                await HandleEventAsync(domainEvent, ct);
            });
        }

        await Task.CompletedTask;
    }
}
```

### 4. Multiple Event Publishers

You can compose multiple event publishers:

```csharp
public class CompositeEventPublisher : IEventPublisher
{
    private readonly IEnumerable<IEventPublisher> _publishers;

    public CompositeEventPublisher(IEnumerable<IEventPublisher> publishers)
    {
        _publishers = publishers;
    }

    public async Task PublishDomainEventsAsync(
        IEnumerable<DomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var publisher in _publishers)
        {
            await publisher.PublishDomainEventsAsync(events, cancellationToken);
        }
    }
}

// Registration
builder.Services.AddSingleton<RabbitMQEventPublisher>();
builder.Services.AddSingleton<EmailNotificationEventPublisher>();
builder.Services.AddSingleton<IEventPublisher>(sp =>
{
    var publishers = new IEventPublisher[]
    {
        sp.GetRequiredService<RabbitMQEventPublisher>(),
        sp.GetRequiredService<EmailNotificationEventPublisher>()
    };
    return new CompositeEventPublisher(publishers);
});
```

## Troubleshooting

### Events Not Being Published

**Problem**: Your event handler is not being called.

**Solutions**:
1. Ensure `IEventPublisher` is registered in DI
2. Check that FileService is receiving the event publisher (not null)
3. Verify events are being raised by the domain (check `fileMetadata.DomainEvents.Count`)
4. Add logging to confirm registration:
   ```csharp
   builder.Services.AddSingleton<IEventPublisher>(sp =>
   {
       var logger = sp.GetRequiredService<ILogger<MyEventPublisher>>();
       logger.LogInformation("Registering MyEventPublisher");
       return new MyEventPublisher(logger);
   });
   ```

### Database Transaction Issues

**Problem**: Event published but database transaction rolled back.

**Solution**: Events are published AFTER `SaveChangesAsync` commits. If you see this, check for exceptions after event publishing.

### Circular Dependency

**Problem**: Cannot inject `IFileService` into event publisher.

**Solution**: Use `IServiceProvider` to resolve lazily:
```csharp
public class MyEventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public async Task PublishDomainEventsAsync(...)
    {
        using var scope = _serviceProvider.CreateScope();
        var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
        // Use fileService...
    }
}
```

## Summary

1. **Implement `IEventPublisher`** to handle domain events your way
2. **Register your implementation** in DI
3. **Handle events** as they occur (upload, validate, scan, reject, delete)
4. **Follow best practices**: error handling, idempotency, async processing

The SDK handles the rest - raising events and calling your publisher automatically!
