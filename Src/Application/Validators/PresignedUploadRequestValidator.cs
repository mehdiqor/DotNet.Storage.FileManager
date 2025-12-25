using FileManager.Application.DTOs;
using FileManager.Common.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FileManager.Application.Validators;

/// <summary>
/// Validator for presigned upload URL requests with business rules and constraints.
/// </summary>
public sealed class PresignedUploadRequestValidator : AbstractValidator<PresignedUploadRequest>
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

    public PresignedUploadRequestValidator(IOptions<FileManagerOptions> options)
    {
        var maxFileSize = options.Value.MaxFileSizeBytes;
        var maxExpiration = TimeSpan.FromDays(7); // Maximum presigned URL expiration

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

        RuleFor(x => x.ExpiresIn)
            .GreaterThan(TimeSpan.Zero)
            .LessThanOrEqualTo(maxExpiration);

        RuleFor(x => x.MaxSize)
            .Must(size => size is null or > 0)
            .WithMessage("Maximum file size must be greater than 0 if specified")
            .Must(size => size == null || size <= maxFileSize)
            .WithMessage($"Maximum file size must not exceed {FormatBytes(maxFileSize)}");
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