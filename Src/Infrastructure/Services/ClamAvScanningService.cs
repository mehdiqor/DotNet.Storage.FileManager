using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Common.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

/// <summary>
/// ClamAV-based virus scanning service implementation.
/// Connects to a ClamAV daemon (clamd) via TCP to scan files for viruses and malware.
/// </summary>
/// <remarks>
/// ClamAV Protocol:
/// - PING: Health check
/// - INSTREAM: Scan a stream of data
/// - VERSION: Get ClamAV version
///
/// The INSTREAM command format:
/// 1. Send "zINSTREAM\0"
/// 2. Send chunks as: [4-byte chunk size][chunk data]
/// 3. Send 4 zero bytes to indicate end of stream
/// 4. Read response
///
/// Response format:
/// - "stream: OK\0" - File is clean
/// - "stream: [virus name] FOUND\0" - File is infected
/// </remarks>
public sealed class ClamAvScanningService(
    IOptions<ClamAvOptions> options,
    ILogger<ClamAvScanningService> logger,
    IMemoryCache? cache = null
) : IVirusScanningService
{
    private readonly ClamAvOptions _options = options.Value;

    private const string PingCommand = "zPING\0";
    private const string InstreamCommand = "zINSTREAM\0";
    private const int ChunkSize = 8192; // 8 KB chunks

    public async Task<ScanResult> ScanAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        logger.LogInformation("Starting virus scan for file: {FileName}", fileName);

        // Check file size limit
        if (_options.MaxFileSizeBytes > 0 && content.Length > _options.MaxFileSizeBytes)
        {
            logger.LogWarning(
                "File {FileName} exceeds maximum scan size: {Size} bytes (limit: {Limit} bytes)",
                fileName,
                content.Length,
                _options.MaxFileSizeBytes);

            return new ScanResult(
                IsClean: false,
                ThreatName: "FILE_TOO_LARGE",
                ScanDetails: new Dictionary<string, string>
                {
                    ["Reason"] = "File exceeds maximum scan size",
                    ["FileSize"] = content.Length.ToString(),
                    ["MaxSize"] = _options.MaxFileSizeBytes.ToString()
                });
        }

        // Check cache if enabled
        string? cacheKey = null;
        if (_options.EnableCaching && cache != null && content.CanSeek)
        {
            cacheKey = await ComputeFileCacheKeyAsync(content, cancellationToken);

            if (cache.TryGetValue<ScanResult>(cacheKey, out var cachedResult) && cachedResult != null)
            {
                logger.LogInformation("Returning cached scan result for file: {FileName}", fileName);
                return cachedResult;
            }
        }

        // Reset stream position
        if (content.CanSeek && content.Position != 0)
            content.Position = 0;

        var retries = 0;
        Exception? lastException = null;

        while (retries <= _options.MaxRetries)
        {
            try
            {
                var result = await ScanWithClamAvAsync(content, fileName, cancellationToken);

                // Cache the result if caching is enabled
                if (!_options.EnableCaching || cache == null || cacheKey == null)
                    return result;

                cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes)
                });

                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                retries++;

                if (retries <= _options.MaxRetries)
                {
                    logger.LogWarning(
                        ex,
                        "Scan failed for file {FileName}. Retry {Retry}/{MaxRetries}",
                        fileName,
                        retries,
                        _options.MaxRetries);

                    // Reset stream position for retry
                    if (content.CanSeek) content.Position = 0;

                    // Exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retries)), cancellationToken);
                }
            }
        }

        logger.LogError(
            lastException,
            "Failed to scan file {FileName} after {MaxRetries} retries",
            fileName,
            _options.MaxRetries);

        throw new InvalidOperationException(
            $"Failed to scan file '{fileName}' after {_options.MaxRetries} retries. ClamAV may be unavailable.",
            lastException);
    }

    private async Task<ScanResult> ScanWithClamAvAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var client = new TcpClient();

        // Set connection timeout
        var connectTask = client.ConnectAsync(_options.Server, _options.Port, cancellationToken).AsTask();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds), cancellationToken);

        var completedTask = await Task.WhenAny(connectTask, timeoutTask);
        if (completedTask == timeoutTask)
            throw new TimeoutException($"Connection to ClamAV server {_options.Server}:{_options.Port} timed out");

        await connectTask; // Propagate any exceptions

        logger.LogDebug("Connected to ClamAV server at {Server}:{Port}", _options.Server, _options.Port);

        await using var networkStream = client.GetStream();

        // Send INSTREAM command
        var instreamBytes = Encoding.ASCII.GetBytes(InstreamCommand);
        await networkStream.WriteAsync(instreamBytes, cancellationToken);

        // Send file content in chunks
        var buffer = new byte[ChunkSize];
        var totalBytesSent = 0L;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.ScanTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            int bytesRead;
            while ((bytesRead = await content.ReadAsync(buffer, linkedCts.Token)) > 0)
            {
                // Send chunk size (4 bytes, big-endian)
                var sizeBytes = BitConverter.GetBytes(bytesRead);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(sizeBytes);
                }

                await networkStream.WriteAsync(sizeBytes, linkedCts.Token);

                // Send chunk data
                await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), linkedCts.Token);

                totalBytesSent += bytesRead;
            }

            // Send terminator (4 zero bytes)
            await networkStream.WriteAsync(new byte[4], linkedCts.Token);
            await networkStream.FlushAsync(linkedCts.Token);

            logger.LogDebug("Sent {Bytes} bytes to ClamAV for scanning", totalBytesSent);

            // Read response
            var responseBuffer = new byte[2048];
            var responseBytesRead = await networkStream.ReadAsync(responseBuffer, linkedCts.Token);
            var response = Encoding.ASCII.GetString(responseBuffer, 0, responseBytesRead).Trim('\0');

            logger.LogDebug("ClamAV response: {Response}", response);

            // Parse response
            var scanDetails = new Dictionary<string, string>
            {
                ["Server"] = $"{_options.Server}:{_options.Port}",
                ["BytesScanned"] = totalBytesSent.ToString(),
                ["ScanTime"] = DateTime.UtcNow.ToString("O")
            };

            if (response.Contains("OK", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("File {FileName} is clean", fileName);

                return new ScanResult(
                    IsClean: true,
                    ThreatName: null,
                    ScanDetails: scanDetails);
            }

            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            {
                // Extract virus name from response
                // Format: "stream: [virus name] FOUND"
                var threatName = ExtractThreatName(response);

                logger.LogWarning(
                    "File {FileName} is infected with: {ThreatName}",
                    fileName,
                    threatName);

                scanDetails["DetectionDetails"] = response;

                return new ScanResult(
                    IsClean: false,
                    ThreatName: threatName,
                    ScanDetails: scanDetails);
            }

            logger.LogError("Unexpected ClamAV response: {Response}", response);
            throw new InvalidOperationException($"Unexpected ClamAV response: {response}");
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Virus scan timed out after {_options.ScanTimeoutSeconds} seconds for file: {fileName}");
        }
    }

    private static string ExtractThreatName(string response)
    {
        // Response format: "stream: [virus name] FOUND"
        var parts = response.Split(':', 2);
        if (parts.Length != 2) return "UNKNOWN_THREAT";

        var virusPart = parts[1].Trim();
        // Remove " FOUND" suffix
        var foundIndex = virusPart.LastIndexOf("FOUND", StringComparison.OrdinalIgnoreCase);
        return foundIndex > 0 ? virusPart[..foundIndex].Trim() : virusPart;
    }

    private static async Task<string> ComputeFileCacheKeyAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
            throw new InvalidOperationException("Stream must be seekable for caching");

        var originalPosition = content.Position;
        content.Position = 0;

        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(content, cancellationToken);
            return $"clamav_scan:{Convert.ToHexString(hashBytes)}";
        }
        finally
        {
            content.Position = originalPosition;
        }
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.Server, _options.Port, cancellationToken);

            await using var networkStream = client.GetStream();

            // Send PING command
            var pingBytes = Encoding.ASCII.GetBytes(PingCommand);
            await networkStream.WriteAsync(pingBytes, cancellationToken);

            // Read response
            var buffer = new byte[256];
            var bytesRead = await networkStream.ReadAsync(buffer, cancellationToken);
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim('\0');

            var isAvailable = response.Contains("PONG", StringComparison.OrdinalIgnoreCase);

            logger.LogInformation(
                "ClamAV server ping {Status}: {Response}",
                isAvailable ? "succeeded" : "failed",
                response);

            return isAvailable;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ping ClamAV server at {Server}:{Port}", _options.Server, _options.Port);
            return false;
        }
    }
}