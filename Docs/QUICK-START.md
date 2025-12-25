# Quick Start Guide

Get started with FileManager SDK in under 5 minutes.

## Prerequisites

- .NET 9.0 SDK
- A supported database (SQL Server, PostgreSQL, MySQL, or SQLite)
- MinIO, SeaweedFS, or AWS S3 account

## Installation

### 1. Install the Package

```bash
dotnet add package FileManager.SDK
```

### 2. Run Database Migrations

```bash
dotnet ef migrations add InitialCreate --project YourProject.csproj
dotnet ef database update --project YourProject.csproj
```

## Configuration

### 1. Add Configuration to appsettings.json

```json
{
  "FileManager": {
    "DefaultProvider": "MinIo",
    "ValidationEnabled": true,
    "VirusScanningEnabled": false,
    "PresignedUrlExpiration": "01:00:00",
    "MaxFileSizeBytes": 5368709120
  },
  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "files",
      "UseSSL": false
    }
  },
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=FileManager;Trusted_Connection=True;"
  }
}
```

### 2. Register Services

In `Program.cs`:

```csharp
using FileManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register FileManager SDK
builder.Services.AddFileManager(builder.Configuration);

// Add other services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Usage

### Create a Controller

```csharp
using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace YourApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileService fileService,
        ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        using var stream = file.OpenReadStream();

        var request = new UploadRequest(
            Content: stream,
            FileName: file.FileName,
            Path: "/uploads",
            Size: file.Length,
            ContentType: file.ContentType,
            Hash: null,
            Metadata: null
        );

        var fileMetadata = await _fileService.UploadFileAsync(request);

        _logger.LogInformation(
            "File uploaded successfully: {FileId}",
            fileMetadata.Id);

        return Ok(new FileUploadResponse
        {
            FileId = fileMetadata.Id,
            FileName = fileMetadata.FileName,
            Status = fileMetadata.Status.ToString(),
            Size = fileMetadata.Size
        });
    }

    /// <summary>
    /// Download a file by ID
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var stream = await _fileService.DownloadFileAsync(id);
            var metadata = await _fileService.GetFileMetadataAsync(id);

            if (metadata == null)
                return NotFound($"File {id} not found");

            return File(stream, metadata.ContentType, metadata.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound($"File {id} not found");
        }
        catch (FileNotAvailableException ex)
        {
            return StatusCode(
                StatusCodes.Status425TooEarly,
                $"File not ready: {ex.CurrentStatus}");
        }
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FileMetadataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMetadata(Guid id)
    {
        var file = await _fileService.GetFileMetadataAsync(id);

        if (file == null)
            return NotFound($"File {id} not found");

        return Ok(new FileMetadataResponse
        {
            Id = file.Id,
            FileName = file.FileName,
            Path = file.Path,
            Size = file.Size,
            ContentType = file.ContentType,
            Status = file.Status.ToString(),
            UploadedAt = file.UploadedAt
        });
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _fileService.DeleteFileAsync(id);
            _logger.LogInformation("File deleted: {FileId}", id);
            return NoContent();
        }
        catch (FileNotFoundException)
        {
            return NotFound($"File {id} not found");
        }
    }
}

// Response models
public record FileUploadResponse
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long Size { get; init; }
}

public record FileMetadataResponse
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}
```

## Test Your API

### 1. Start Your Application

```bash
dotnet run
```

### 2. Upload a File

```bash
curl -X POST http://localhost:5000/api/files/upload \
  -F "file=@test.pdf" \
  -H "Accept: application/json"
```

Response:
```json
{
  "fileId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "test.pdf",
  "status": "Pending",
  "size": 102400
}
```

### 3. Get File Metadata

```bash
curl http://localhost:5000/api/files/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

Response:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "test.pdf",
  "path": "/uploads",
  "size": 102400,
  "contentType": "application/pdf",
  "status": "Pending",
  "uploadedAt": "2025-12-25T10:30:00Z"
}
```

### 4. Download File

```bash
curl -o downloaded.pdf \
  http://localhost:5000/api/files/3fa85f64-5717-4562-b3fc-2c963f66afa6/download
```

### 5. Delete File

```bash
curl -X DELETE \
  http://localhost:5000/api/files/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## Running with Docker

### Docker Compose for Development

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  minio:
    image: minio/minio:latest
    command: server /data --console-address ":9001"
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    volumes:
      - minio_data:/data

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourStrong@Password
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  minio_data:
  sqlserver_data:
```

Start services:

```bash
docker-compose up -d
```

Update `appsettings.json`:

```json
{
  "Storage": {
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "files",
      "UseSSL": false
    }
  },
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost,1433;Database=FileManager;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  }
}
```

## Next Steps

### Essential Reading

1. **[Usage Guide](USAGE-GUIDE.md)** - Comprehensive usage examples
2. **[Configuration](CONFIGURATION.md)** - Complete configuration reference
3. **[Database Setup](DATABASE-CONFIGURATION.md)** - Database configuration guide

### Advanced Topics

4. **[Architecture](ARCHITECTURE.md)** - Understand the architecture
5. **[Dependency Injection](DI-USAGE-EXAMPLE.md)** - Advanced DI patterns
6. **[Performance Tuning](PERFORMANCE.md)** - Optimize for production

### Common Scenarios

- **File Validation**: Implement custom validation logic
- **Virus Scanning**: Integrate antivirus services
- **Batch Processing**: Process pending files in background
- **Multi-Provider**: Use multiple storage providers

## Troubleshooting

### Connection Issues

**Problem**: Cannot connect to MinIO
```
StorageProviderException: Connection refused
```

**Solution**: Ensure MinIO is running and accessible:
```bash
curl http://localhost:9000/minio/health/live
```

### Database Migration Errors

**Problem**: Migration fails
```
InvalidOperationException: No database provider configured
```

**Solution**: Verify database configuration in `appsettings.json`:
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "..."
  }
}
```

### Upload Validation Errors

**Problem**: Upload rejected
```
ValidationException: File size exceeds maximum
```

**Solution**: Check `MaxFileSizeBytes` in configuration or adjust file size.

## Support

- 📖 [Full Documentation](../README.md)
- 💬 [GitHub Discussions](https://github.com/your-org/filemanager-sdk/discussions)
- 🐛 [Report Issues](https://github.com/your-org/filemanager-sdk/issues)

---

**Congratulations!** You've successfully set up FileManager SDK. Check out the [Usage Guide](USAGE-GUIDE.md) for more advanced scenarios.
