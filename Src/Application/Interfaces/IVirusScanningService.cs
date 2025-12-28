using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

/// <summary>
/// Service for scanning files for viruses and malware.
/// Supports ClamAV, Windows Defender, VirusTotal, and other virus scanners.
/// </summary>
/// <remarks>
/// Example implementations: ClamAV, Windows Defender API, VirusTotal API,
/// McAfee, Sophos, or custom ML-based malware detection.
///
/// Performance considerations:
/// - Implement timeouts for scan operations
/// - Set file size limits to avoid scanning extremely large files
/// - Use async/background processing for large files
/// - Cache scan results based on file hash
/// </remarks>
/// <example>
/// <code>
/// // Example ClamAV implementation
/// public class ClamAvScanningService : IVirusScanningService
/// {
///     private readonly TcpClient _client;
///
///     public async Task&lt;ScanResult&gt; ScanAsync(
///         Stream content,
///         string fileName,
///         CancellationToken cancellationToken = default)
///     {
///         // Send file to ClamAV daemon and get scan result
///         var response = await ScanWithClamAvAsync(content, cancellationToken);
///         var isClean = response.Contains("OK");
///
///         return new ScanResult(
///             IsClean: isClean,
///             ThreatName: isClean ? null : ExtractThreatName(response));
///     }
///
///     public async Task&lt;bool&gt; HealthAsync(CancellationToken cancellationToken = default)
///     {
///         // Send PING command to ClamAV daemon
///         var response = await SendCommandAsync("PING", cancellationToken);
///         return response.Contains("PONG");
///     }
/// }
/// </code>
/// </example>
public interface IVirusScanningService
{
    /// <summary>
    /// Scans a file stream for viruses and malware.
    /// </summary>
    /// <param name="content">
    /// The file content stream to scan. The stream should be seekable and positioned at the beginning.
    /// The implementation is responsible for handling stream positioning and disposal if needed.
    /// </param>
    /// <param name="fileName">
    /// The name of the file being scanned. Can be used for logging or file type detection.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the scan operation.</param>
    /// <returns>
    /// A task that represents the asynchronous scan operation.
    /// The task result contains the scan result indicating whether the file is clean and any threat details.
    /// </returns>
    /// <remarks>
    /// Implementations should:
    /// - Handle scan timeouts appropriately
    /// - Log scan operations for audit purposes
    /// - Consider file size limits to avoid excessive resource usage
    /// - Reset stream position to the beginning if the stream needs to be reused
    /// - Handle scanner unavailability gracefully (e.g., return error or retry)
    ///
    /// Performance considerations:
    /// - Scanning can be slow for large files
    /// - Consider implementing a file size threshold
    /// - Cache scan results based on file hash to avoid re-scanning identical files
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the scanning service is unavailable or misconfigured.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the scan operation is cancelled via the cancellation token.
    /// </exception>
    Task<ScanResult> ScanAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the virus scanning service is available and responding.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the health check operation.</param>
    /// <returns>
    /// A task that represents the asynchronous health check operation.
    /// Returns true if the scanner is healthy and available, false otherwise.
    /// </returns>
    /// <remarks>
    /// Use this method to verify scanner availability before performing scans,
    /// or to implement health checks in monitoring systems.
    /// This method should not throw exceptions; return false for any errors.
    /// </remarks>
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}