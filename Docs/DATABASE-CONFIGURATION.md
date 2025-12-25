# Database Configuration Guide

The File Manager SDK supports four database providers with transparent switching via configuration.

## Supported Providers

| Provider | Package | Version | Use Case |
|----------|---------|---------|----------|
| **SQL Server** | Microsoft.EntityFrameworkCore.SqlServer | 9.0.10 | Production (default) |
| **PostgreSQL** | Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.3 | Production (cross-platform) |
| **MySQL** | Pomelo.EntityFrameworkCore.MySql | 9.0.0 | Production (open-source) |
| **SQLite** | Microsoft.EntityFrameworkCore.Sqlite | 9.0.10 | Development/Testing |

## Configuration

### SQL Server (Default)

```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=FileManager;Trusted_Connection=True;MultipleActiveResultSets=true",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 30,
    "CommandTimeoutSeconds": 30
  }
}
```

### PostgreSQL

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Port=5432;Database=filemanager;Username=postgres;Password=yourpassword",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 30,
    "CommandTimeoutSeconds": 30
  }
}
```

### MySQL

```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=localhost;Port=3306;Database=filemanager;Uid=root;Pwd=yourpassword",
    "EnableDetailedErrors": false,
    "EnableSensitiveDataLogging": false,
    "MaxRetryCount": 3,
    "MaxRetryDelaySeconds": 30,
    "CommandTimeoutSeconds": 30
  }
}
```

### SQLite (Development)

```json
{
  "Database": {
    "Provider": "SqLite",
    "ConnectionString": "Data Source=filemanager.db",
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": true,
    "MaxRetryCount": 0,
    "MaxRetryDelaySeconds": 0,
    "CommandTimeoutSeconds": 30
  }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Provider` | Enum | SqlServer | Database provider to use |
| `ConnectionString` | string | (required) | Provider-specific connection string |
| `EnableDetailedErrors` | bool | false | Show detailed error messages (dev only) |
| `EnableSensitiveDataLogging` | bool | false | Log parameter values (dev only) |
| `MaxRetryCount` | int | 3 | Maximum retry attempts for transient failures |
| `MaxRetryDelaySeconds` | int | 30 | Maximum delay between retries |
| `CommandTimeoutSeconds` | int | 30 | Database command timeout |

## Database Migrations

### Creating Migrations

```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project Src/FileManager.csproj

# Update database
dotnet ef database update --project Src/FileManager.csproj

# Generate SQL script
dotnet ef migrations script --project Src/FileManager.csproj --output migrations.sql
```

### Provider-Specific Migrations

To create migrations for a specific provider, temporarily set that provider in `appsettings.json`:

```bash
# For PostgreSQL
# 1. Change appsettings.json: "Provider": "PostgreSql"
# 2. Create migration
dotnet ef migrations add InitialCreate --project Src/FileManager.csproj

# For MySQL
# 1. Change appsettings.json: "Provider": "MySql"
# 2. Create migration
dotnet ef migrations add InitialCreate --project Src/FileManager.csproj
```

## Connection String Examples

### SQL Server

**LocalDB:**
```
Server=(localdb)\\mssqllocaldb;Database=FileManager;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server (Windows Auth):**
```
Server=localhost;Database=FileManager;Integrated Security=True;TrustServerCertificate=True
```

**SQL Server (SQL Auth):**
```
Server=localhost;Database=FileManager;User Id=sa;Password=YourPassword123;TrustServerCertificate=True
```

**Azure SQL:**
```
Server=tcp:yourserver.database.windows.net,1433;Database=FileManager;User ID=yourusername;Password=yourpassword;Encrypt=True
```

### PostgreSQL

**Local:**
```
Host=localhost;Port=5432;Database=filemanager;Username=postgres;Password=yourpassword
```

**With SSL:**
```
Host=localhost;Port=5432;Database=filemanager;Username=postgres;Password=yourpassword;SSL Mode=Require
```

**Cloud (Azure/AWS):**
```
Host=yourserver.postgres.database.azure.com;Port=5432;Database=filemanager;Username=admin@yourserver;Password=yourpassword;SSL Mode=Require
```

### MySQL

**Local:**
```
Server=localhost;Port=3306;Database=filemanager;Uid=root;Pwd=yourpassword
```

**With SSL:**
```
Server=localhost;Port=3306;Database=filemanager;Uid=root;Pwd=yourpassword;SslMode=Required
```

**Cloud (AWS RDS):**
```
Server=yourinstance.us-east-1.rds.amazonaws.com;Port=3306;Database=filemanager;Uid=admin;Pwd=yourpassword;SslMode=Required
```

### SQLite

**File-based:**
```
Data Source=filemanager.db
```

**In-memory (testing):**
```
Data Source=:memory:
```

**With encryption:**
```
Data Source=filemanager.db;Password=yourpassword
```

## Resilience Features

All providers (except SQLite) include automatic retry logic for transient failures:

- **Automatic Retry**: Failed operations are retried automatically
- **Exponential Backoff**: Delay between retries increases exponentially
- **Circuit Breaker**: Prevents cascading failures
- **Connection Pooling**: Efficient connection reuse

## Performance Optimization

### Indexes

The SDK automatically creates optimized indexes:
- `StorageKey` (unique)
- `Hash`
- `Status`
- `Provider`
- `UploadedAt`
- `Status + UploadedAt` (composite)

### Query Optimization

- Uses compiled queries for frequently executed queries
- Implements proper pagination
- Uses projections to minimize data transfer
- Tracks only when necessary

## Switching Providers

To switch database providers:

1. Update `appsettings.json` with new provider and connection string
2. Create migrations for the new provider
3. Update the database
4. Restart the application

**Note**: Data migration between providers requires custom scripts or ETL tools.

## Troubleshooting

### Connection Issues

**SQL Server:**
- Ensure SQL Server is running
- Check firewall rules
- Verify TCP/IP is enabled

**PostgreSQL:**
- Check `pg_hba.conf` for authentication settings
- Ensure PostgreSQL service is running
- Verify port 5432 is accessible

**MySQL:**
- Check MySQL service status
- Verify user permissions
- Ensure port 3306 is open

### Migration Issues

**Provider-specific types:**
Some data types are provider-specific and may require custom value converters.

**Naming conventions:**
Different providers have different identifier casing rules. The SDK handles this automatically.

## Best Practices

1. **Production**: Use SQL Server, PostgreSQL, or MySQL
2. **Development**: Use SQLite for quick setup
3. **CI/CD**: Use in-memory SQLite for fast tests
4. **Migrations**: Test on all target providers before deploying
5. **Connection Strings**: Store in environment variables or secret managers
6. **Monitoring**: Enable detailed errors only in development
7. **Performance**: Use connection pooling and proper indexing

## References

- [EF Core Providers](https://learn.microsoft.com/en-us/ef/core/providers/)
- [SQL Server Best Practices](https://learn.microsoft.com/en-us/sql/relational-databases/performance/performance-best-practices)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [MySQL Documentation](https://dev.mysql.com/doc/)
