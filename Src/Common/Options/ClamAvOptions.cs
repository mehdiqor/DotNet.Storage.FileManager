namespace FileManager.Common.Options;

/// <summary>
/// Configuration options for ClamAV virus scanning service.
/// </summary>
public sealed class ClamAvOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "ClamAV";

    /// <summary>
    /// ClamAV server hostname or IP address.
    /// Default: localhost
    /// </summary>
    public string Server { get; set; } = "localhost";

    /// <summary>
    /// ClamAV daemon port.
    /// Default: 3310
    /// </summary>
    public int Port { get; set; } = 3310;

    /// <summary>
    /// Maximum file size to scan in bytes.
    /// Files larger than this will be skipped or rejected.
    /// Default: 100 MB (104857600 bytes)
    /// Set to 0 for no limit.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 104_857_600; // 100 MB

    /// <summary>
    /// Timeout for scan operations in seconds.
    /// Default: 60 seconds
    /// </summary>
    public int ScanTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Connection timeout in seconds.
    /// Default: 10 seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of retries for failed scans.
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether to enable scan result caching based on file hash.
    /// This can improve performance for duplicate files.
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes.
    /// Only used when EnableCaching is true.
    /// Default: 60 minutes (1 hour)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;
}