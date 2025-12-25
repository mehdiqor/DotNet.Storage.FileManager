# SeaweedFS Webhook Setup

This project provides a complete setup for SeaweedFS with HTTP webhook and RabbitMQ notifications, plus a .NET 8 API to handle events.

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  SeaweedFS      │     │  SeaweedFS      │     │  SeaweedFS      │
│  Master         │────▶│  Volume         │────▶│  Filer          │
│  :9333          │     │  :8080          │     │  :8888          │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
                                                         │
                                    ┌────────────────────┼────────────────────┐
                                    │                    │                    │
                                    ▼                    ▼                    │
                        ┌─────────────────┐  ┌─────────────────┐              │
                        │  HTTP Webhook   │  │  RabbitMQ       │              │
                        │  (sync)         │  │  (async)        │              │
                        └────────┬────────┘  └────────┬────────┘              │
                                 │                    │                       │
                                 └────────────────────┼───────────────────────┘
                                                      │
                                                      ▼
                                          ┌─────────────────┐
                                          │  .NET Webhook   │
                                          │  API :5000      │
                                          └─────────────────┘
```

## Components

| Component | Image/Version | Port | Description |
|-----------|---------------|------|-------------|
| Master | `chrislusf/seaweedfs:3.59` | 9333 | Cluster management |
| Volume | `chrislusf/seaweedfs:3.59` | 8080 | File storage |
| Filer | `chrislusf/seaweedfs:3.59` | 8888 | File system abstraction |
| S3 Gateway | `chrislusf/seaweedfs:3.59` | 8333 | S3-compatible API |
| RabbitMQ | `rabbitmq:3.12-management-alpine` | 5672, 15672 | Message broker |
| Webhook API | .NET 8 | 5000 | Event handler |

## .NET Package Requirements

```xml
<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
<PackageReference Include="System.Text.Json" Version="8.0.4" />
<PackageReference Include="AspNetCore.HealthChecks.Rabbitmq" Version="8.0.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

## Quick Start

### 1. Start the services

```bash
# Clone or create the project structure
cd seaweedfs-setup

# Start all services
docker-compose up -d

# Check logs
docker-compose logs -f
```

### 2. Verify services are running

```bash
# Check SeaweedFS master
curl http://localhost:9333/cluster/status

# Check filer
curl http://localhost:8888/

# Check RabbitMQ management
open http://localhost:15672  # Login: seaweed / seaweed123

# Check webhook API
curl http://localhost:5000/health
```

### 3. Upload a test file

```bash
# Upload via filer HTTP API
curl -F "file=@test.txt" http://localhost:8888/uploads/

# Upload via S3 (using AWS CLI)
aws --endpoint-url http://localhost:8333 s3 cp test.txt s3://bucket/test.txt
```

### 4. Check webhook logs

```bash
docker-compose logs -f webhook-api
```

## Authentication

The webhook API supports multiple authentication methods:

### 1. Query Parameter (Used by SeaweedFS)

SeaweedFS doesn't support custom headers in notifications, so we use a query parameter:

```
url = "http://webhook-api:5000/api/seaweedfs/webhook?secret=your-secret-key"
```

### 2. X-Webhook-Secret Header

For external clients:

```bash
curl -X POST http://localhost:5000/api/seaweedfs/webhook \
  -H "X-Webhook-Secret: your-super-secret-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{"directory": "/test", "name": "file.txt"}'
```

### 3. Bearer Token

```bash
curl -X POST http://localhost:5000/api/seaweedfs/webhook \
  -H "Authorization: Bearer your-super-secret-key-change-in-production" \
  -H "Content-Type: application/json" \
  -d '{"directory": "/test", "name": "file.txt"}'
```

### 4. HMAC Signature (X-Hub-Signature-256)

For GitHub-style webhook verification:

```bash
PAYLOAD='{"directory": "/test", "name": "file.txt"}'
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -hmac "your-secret-key" | awk '{print $2}')

curl -X POST http://localhost:5000/api/seaweedfs/webhook \
  -H "X-Hub-Signature-256: sha256=$SIGNATURE" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD"
```

## Event Format

SeaweedFS sends events in this format:

```json
{
  "directory": "/uploads",
  "name": "document.pdf",
  "isDirectory": false,
  "oldEntry": null,
  "newEntry": {
    "name": "document.pdf",
    "isDirectory": false,
    "chunks": [
      {
        "file_id": "1,01234567890abc",
        "size": 1024,
        "mtime": 1699999999
      }
    ],
    "attributes": {
      "mtime": 1699999999,
      "crtime": 1699999999,
      "mode": 420,
      "uid": 0,
      "gid": 0,
      "mime": "application/pdf",
      "fileSize": 1024
    }
  }
}
```

### Event Types

| Event | oldEntry | newEntry | Description |
|-------|----------|----------|-------------|
| Create | null | present | New file uploaded |
| Update | present | present | File modified |
| Delete | present | null | File deleted |

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `Webhook__SecretKey` | - | Secret key for authentication |
| `Webhook__ValidateSignature` | `true` | Enable/disable auth validation |
| `RabbitMQ__HostName` | `localhost` | RabbitMQ host |
| `RabbitMQ__Port` | `5672` | RabbitMQ port |
| `RabbitMQ__UserName` | `guest` | RabbitMQ username |
| `RabbitMQ__Password` | `guest` | RabbitMQ password |
| `RabbitMQ__Exchange` | `seaweedfs` | Exchange name |
| `RabbitMQ__Queue` | `seaweedfs_events` | Queue name |

### SeaweedFS filer.toml

Key notification settings:

```toml
# HTTP webhook
[notification.http]
enabled = true
url = "http://webhook-api:5000/api/seaweedfs/webhook?secret=your-key"
timeout = 10

# RabbitMQ
[notification.amqp]
enabled = true
url = "amqp://user:pass@rabbitmq:5672/"
exchange = "seaweedfs"
exchange_type = "fanout"
queue_name = "seaweedfs_events"
queue_durable = true
```

## Extending the Event Handler

Edit `Services/SeaweedEventHandler.cs` to add your business logic:

```csharp
private async Task HandleFileCreatedAsync(SeaweedFilerEvent seaweedEvent, CancellationToken cancellationToken)
{
    var mimeType = seaweedEvent.NewEntry?.Attributes?.MimeType ?? string.Empty;
    
    // Example: Process images
    if (mimeType.StartsWith("image/"))
    {
        await _imageService.GenerateThumbnailAsync(seaweedEvent.FullPath, cancellationToken);
    }
    
    // Example: Index documents for search
    if (mimeType == "application/pdf")
    {
        await _searchService.IndexDocumentAsync(seaweedEvent.FullPath, cancellationToken);
    }
    
    // Example: Send notification
    await _notificationService.SendAsync($"New file uploaded: {seaweedEvent.FullPath}", cancellationToken);
}
```

## Troubleshooting

### Webhook not receiving events

1. Check filer logs: `docker-compose logs filer`
2. Verify URL is reachable from filer container
3. Check authentication secret matches

### RabbitMQ connection issues

1. Ensure RabbitMQ is healthy: `docker-compose ps`
2. Check credentials match in docker-compose.yml and appsettings.json
3. Verify exchange and queue exist in RabbitMQ management UI

### Events not processing

1. Check webhook-api logs: `docker-compose logs webhook-api`
2. Verify JSON format matches expected model
3. Check for exceptions in event handler

## Production Recommendations

1. **Change all default passwords** in docker-compose.yml and filer.toml
2. **Use HTTPS** for webhook endpoints (use nginx/traefik as reverse proxy)
3. **Enable persistent volumes** for RabbitMQ and SeaweedFS
4. **Set resource limits** in docker-compose.yml
5. **Use secrets management** (Docker secrets, HashiCorp Vault, etc.)
6. **Add retry logic** for failed webhook deliveries
7. **Implement dead letter queue** for RabbitMQ
8. **Add monitoring** (Prometheus metrics are exposed)

## Supported SeaweedFS Versions

Tested with:
- SeaweedFS 3.59 (recommended)
- SeaweedFS 3.50+

The notification format has been stable since version 3.x.
