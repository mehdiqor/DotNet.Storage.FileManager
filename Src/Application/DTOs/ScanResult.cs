namespace FileManager.Application.DTOs;

/// <summary>
/// Represents the result of a virus scan operation.
/// </summary>
/// <param name="IsClean">Indicates whether the file is clean (true) or infected (false).</param>
/// <param name="ThreatName">
/// The name of the detected threat/virus if the file is infected.
/// Null if the file is clean or if the threat name is unknown.
/// </param>
/// <param name="ScanDetails">
/// Optional additional details about the scan (e.g., scanner version, scan duration, etc.).
/// </param>
public sealed record ScanResult(
    bool IsClean,
    string? ThreatName = null,
    Dictionary<string, string>? ScanDetails = null);