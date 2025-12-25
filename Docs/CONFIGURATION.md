# Configuration Reference

Complete configuration reference for FileManager SDK.

## Table of Contents

- [FileManager Options](#filemanager-options)
- [Storage Provider Configuration](#storage-provider-configuration)
- [Database Configuration](#database-configuration)
- [Validation Settings](#validation-settings)
- [Environment-Specific Configuration](#environment-specific-configuration)

## FileManager Options

Configure core FileManager SDK settings in `appsettings.json`:

```json
{
  "FileManager": {
    "DefaultProvider": "MinIo",
    "ValidationEnabled": true,
    "VirusScanningEnabled": false,
    "PresignedUrlExpiration": "01:00:00",
    "MaxFileSizeBytes": 5368709120
  }
}
```

### Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultProvider` | `string` | Required | Storage provider: `MinIo`, `SeaweedFs`, or `S3` |
| `ValidationEnabled` | `bool` | `true` | Enable file validation workflow |
| `VirusScanningEnabled` | `bool` | `false` | Enable virus scanning workflow |
| `PresignedUrlExpiration` | `TimeSpan` | `01:00:00` | Default expiration for presigned URLs (format: `HH:MM:SS`) |
| `MaxFileSizeBytes` | `long` | `5368709120` | Maximum file size in bytes (default: 5 GB) |

## Storage Provider Configuration

### MinIO Configuration

```json
{
  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "files",
      "UseSSL": false,
      "Region": "us-east-1"
    }
  }
}
```

| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `Endpoint` | `string` | ✅ | MinIO server endpoint (without http://) |
| `AccessKey` | `string` | ✅ | MinIO access key |
| `SecretKey` | `string` | ✅ | MinIO secret key |
| `BucketName` | `string` | ✅ | Bucket name for file storage |
| `UseSSL` | `bool` | ❌ | Use HTTPS (default: `false`) |
| `Region` | `string` | ❌ | AWS region (default: `us-east-1`) |

### SeaweedFS Configuration

```json
{
  "Storage": {
    "SeaweedFS": {
      "FilerUrl": "http://localhost:8888",
      "BucketName": "files",
      "AccessKey": null,
      "SecretKey": null
    }
  }
}
```

| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `FilerUrl` | `string` | ✅ | SeaweedFS Filer URL |
| `BucketName` | `string` | ✅ | Bucket/collection name |
| `AccessKey` | `string` | ❌ | Optional access key for S3 API |
| `SecretKey` | `string` | ❌ | Optional secret key for S3 API |

### AWS S3 Configuration

```json
{
  "Storage": {
    "S3": {
      "ServiceUrl": "https://s3.amazonaws.com",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "BucketName": "your-bucket",
      "Region": "us-east-1",
      "UsePathStyle": false
    }
  }
}
```

| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `ServiceUrl` | `string` | ✅ | S3 service URL |
| `AccessKey` | `string` | ✅ | AWS access key |
| `SecretKey` | `string` | ✅ | AWS secret key |
| `BucketName` | `string` | ✅ | S3 bucket name |
| `Region` | `string` | ✅ | AWS region |
| `UsePathStyle` | `bool` | ❌ | Use path-style URLs (default: `false`) |

## Database Configuration

Configure database provider and connection:

```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=FileManager;Trusted_Connection=True;",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 30,
    "CommandTimeoutSeconds": 30
  }
}
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Provider` | `string` | Required | Database provider: `SqlServer`, `PostgreSql`, `MySql`, or `SqLite` |
| `ConnectionString` | `string` | Required | Database connection string |
| `EnableDetailedErrors` | `bool` | `false` | Show detailed EF Core errors (dev only) |
| `EnableSensitiveDataLogging` | `bool` | `false` | Log sensitive data (dev only) |
| `MaxRetryCount` | `int` | `3` | Max connection retry attempts |
| `MaxRetryDelaySeconds` | `int` | `30` | Max delay between retries |
| `CommandTimeoutSeconds` | `int` | `30` | Command execution timeout |

### Database-Specific Connection Strings

**SQL Server:**
```json
"ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=FileManager;Trusted_Connection=True;"
```

**PostgreSQL:**
```json
"ConnectionString": "Host=localhost;Database=FileManager;Username=postgres;Password=password"
```

**MySQL:**
```json
"ConnectionString": "Server=localhost;Database=FileManager;User=root;Password=password"
```

**SQLite:**
```json
"ConnectionString": "Data Source=filemanager.db"
```

See [Database Configuration Guide](DATABASE-CONFIGURATION.md) for detailed setup instructions.

## Validation Settings

FluentValidation settings are configured in `FileManagerOptions.MaxFileSizeBytes`:

### Allowed Content Types

The SDK validates uploaded files against a whitelist of content types:

- **Documents**: PDF, Word, Excel, PowerPoint
- **Images**: JPEG, PNG, GIF, WebP, SVG
- **Audio**: MP3, WAV, AAC
- **Video**: MP4, WebM, AVI
- **Archives**: ZIP, RAR, 7Z, TAR, GZIP
- **Text**: Plain text, CSV, JSON, XML
- **Code**: JavaScript, TypeScript, Python, etc.

Override in `UploadRequestValidator.cs` if needed.

### File Size Limits

```json
{
  "FileManager": {
    "MaxFileSizeBytes": 5368709120  // 5 GB
  }
}
```

Validation rules:
- Minimum: 1 byte
- Maximum: Configurable (default 5 GB)
- Zero-byte files are rejected

## Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "FileManager": {
    "DefaultProvider": "MinIo",
    "ValidationEnabled": true,
    "VirusScanningEnabled": false
  },
  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "dev-files",
      "UseSSL": false
    }
  },
  "Database": {
    "Provider": "SqLite",
    "ConnectionString": "Data Source=filemanager-dev.db",
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "FileManager": {
    "DefaultProvider": "S3",
    "ValidationEnabled": true,
    "VirusScanningEnabled": true,
    "MaxFileSizeBytes": 10737418240  // 10 GB
  },
  "Storage": {
    "S3": {
      "ServiceUrl": "https://s3.amazonaws.com",
      "AccessKey": "${AWS_ACCESS_KEY}",
      "SecretKey": "${AWS_SECRET_KEY}",
      "BucketName": "prod-filemanager",
      "Region": "us-east-1"
    }
  },
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "${DATABASE_CONNECTION_STRING}",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "MaxRetryCount": 5,
    "MaxRetryDelaySeconds": 60
  }
}
```

### Using Environment Variables

```bash
export FileManager__DefaultProvider="S3"
export Storage__S3__AccessKey="your-key"
export Storage__S3__SecretKey="your-secret"
export Database__ConnectionString="Server=prod-db;..."
```

Or in `appsettings.json`:
```json
{
  "Storage": {
    "S3": {
      "AccessKey": "${AWS_ACCESS_KEY}",
      "SecretKey": "${AWS_SECRET_KEY}"
    }
  }
}
```

## Multi-Provider Configuration

For scenarios requiring multiple storage providers simultaneously:

```csharp
// Register all providers
builder.Services.AddStorageProviderFactory(builder.Configuration);
```

Configure all providers in `appsettings.json`:

```json
{
  "FileManager": {
    "DefaultProvider": "MinIo"
  },
  "Storage": {
    "MinIO": { /* config */ },
    "SeaweedFS": { /* config */ },
    "S3": { /* config */ }
  }
}
```

Usage:

```csharp
public class MigrationService
{
    private readonly IStorageProviderFactory _factory;

    public async Task MigrateAsync()
    {
        var source = _factory.GetStorage(StorageProvider.MinIo);
        var target = _factory.GetStorage(StorageProvider.S3);

        // Migrate files between providers
    }
}
```

See [DI Usage Examples](DI-USAGE-EXAMPLE.md) for more details.

## Configuration Validation

The SDK validates configuration at startup. Invalid configuration throws detailed exceptions:

```
InvalidOperationException: Unsupported storage provider: InvalidProvider.
Valid options are: MinIo, SeaweedFs, S3
```

```
InvalidOperationException: Database connection string is not configured.
Please set 'Database:ConnectionString' in your configuration.
```

## Next Steps

- [Usage Guide](USAGE-GUIDE.md)
- [Database Configuration](DATABASE-CONFIGURATION.md)
- [Dependency Injection](DI-USAGE-EXAMPLE.md)
- [Service Lifetime Guide](SERVICE-LIFETIME-GUIDE.md)
