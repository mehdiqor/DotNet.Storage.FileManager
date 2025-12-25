using FileManager.Domain.Enums;

namespace FileManager.Common.Options;

/// <summary>
/// Database configuration options supporting multiple providers.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// The database provider to use (SqlServer, PostgreSQL, MySQL, or SQLite).
    /// </summary>
    public DatabaseProvider Provider { get; init; } = DatabaseProvider.SqlServer;

    /// <summary>
    /// Connection string for the selected database provider.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Enable detailed error messages (development only).
    /// </summary>
    public bool EnableDetailedErrors { get; init; } = false;

    /// <summary>
    /// Enable sensitive data logging (development only).
    /// </summary>
    public bool EnableSensitiveDataLogging { get; init; } = false;

    /// <summary>
    /// Maximum retry count for transient failures.
    /// </summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>
    /// Maximum delay between retries in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; init; } = 30;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; init; } = 30;
}