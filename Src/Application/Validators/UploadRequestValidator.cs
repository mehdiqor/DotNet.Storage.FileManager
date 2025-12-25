using FileManager.Application.DTOs;
using FileManager.Common.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FileManager.Application.Validators;

/// <summary>
/// Validator for upload requests with business rules and file constraints.
/// </summary>
public sealed class UploadRequestValidator : AbstractValidator<UploadRequest>
{
    private static readonly string[] AllowedContentTypes =
    [
        // Images
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml", "image/bmp",

        // Documents
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",

        // Text
        "text/plain", "text/csv", "text/html", "text/xml",
        "application/json", "application/xml",

        // Archives
        "application/zip", "application/x-rar-compressed", "application/x-7z-compressed",
        "application/gzip", "application/x-tar",

        // Video
        "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo", "video/webm",

        // Audio
        "audio/mpeg", "audio/wav", "audio/ogg", "audio/webm",

        // Other
        "application/octet-stream"
    ];

    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public UploadRequestValidator(IOptions<FileManagerOptions> options)
    {
        var maxFileSize = options.Value.MaxFileSizeBytes;

        RuleFor(x => x.Content)
            .NotNull()
            .Must(stream => stream.CanRead)
            .WithMessage("File content stream must be readable");

        RuleFor(x => x.Path)
            .NotEmpty()
            .MaximumLength(500)
            .Must(path => !path.StartsWith('/') && !path.StartsWith('\\'))
            .WithMessage("File path must be relative (cannot start with / or \\)")
            .Must(BeValidPath)
            .WithMessage("File path contains invalid characters");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255)
            .Must(BeValidFileName)
            .WithMessage("File name contains invalid characters")
            .Must(name => !string.IsNullOrWhiteSpace(Path.GetExtension(name)))
            .WithMessage("File name must have an extension");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(BeValidContentType)
            .WithMessage($"Content type must be one of the allowed types: {string.Join(", ", AllowedContentTypes)}");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .LessThanOrEqualTo(maxFileSize)
            .WithMessage($"File size must not exceed {FormatBytes(maxFileSize)}");

        RuleFor(x => x.Hash)
            .Must(hash => string.IsNullOrEmpty(hash) || IsValidHash(hash))
            .WithMessage("Hash must be a valid hexadecimal string (32, 40, 64, or 128 characters for MD5, SHA1, SHA256, or SHA512)");

        RuleFor(x => x.Metadata)
            .Must(metadata => metadata is not { Count: > 50 })
            .WithMessage("Metadata cannot contain more than 50 entries")
            .Must(metadata => metadata == null || metadata.All(kv => kv.Key.Length <= 100 && kv.Value.Length <= 500))
            .WithMessage("Metadata keys must not exceed 100 characters and values must not exceed 500 characters");
    }

    private static bool BeValidPath(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               !path.Any(c => InvalidPathChars.Contains(c));
    }

    private static bool BeValidFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
               !fileName.Any(c => InvalidFileNameChars.Contains(c));
    }

    private static bool BeValidContentType(string contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) &&
               AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsValidHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return true; // Null or empty is valid (optional field)

        // Valid hash lengths: MD5 (32), SHA1 (40), SHA256 (64), SHA512 (128)
        var validLengths = new[] { 32, 40, 64, 128 };

        return validLengths.Contains(hash.Length) &&
               hash.All(c => char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F');
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}