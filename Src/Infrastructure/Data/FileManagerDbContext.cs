using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Infrastructure.Data;

/// <summary>
/// Database context for the File Manager SDK.
/// Supports multiple database providers (SQL Server, PostgreSQL, MySQL, SQLite).
/// </summary>
public sealed class FileManagerDbContext(DbContextOptions<FileManagerDbContext> options) : DbContext(options)
{
    /// <summary>
    /// File metadata entities.
    /// </summary>
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileManagerDbContext).Assembly);
    }
}