using System.Text;

namespace TaskMaster.Demo.Web.Storage;

/// <summary>
/// Local-filesystem implementation of <see cref="IReportFileStore"/>.
///
/// Keys are laid out as <c>{yyyy}/{MM}/{reportId}.{ext}</c> — a relative, path-agnostic
/// key that is equally valid as an S3 or Azure Blob key, so the same column can back an
/// object store later. Writes are atomic (temp file + move) so a reader can never
/// observe a half-written report.
/// </summary>
public sealed class LocalFileReportStore : IReportFileStore
{
    private readonly string _root;
    private readonly long _maxFileBytes;
    private readonly ILogger<LocalFileReportStore> _logger;

    public LocalFileReportStore(StorageOptions options, ILogger<LocalFileReportStore> logger)
    {
        options.Validate();

        // Resolved once: the key layout stays relative, the physical root is absolute.
        _root = Path.GetFullPath(options.RootPath);
        _maxFileBytes = options.MaxFileBytes;
        _logger = logger;

        // A relative Storage:RootPath resolves against the current working directory, so
        // this process and TaskMaster.Demo.Consumer can silently end up on different
        // roots. Logging the resolved path in both makes that divergence visible in the
        // startup output instead of surfacing later as a 404 on the download.
        _logger.LogInformation(
            "Report store using provider {Provider} and root {StorageRoot} (max {MaxFileBytes} bytes per file)",
            options.Provider, _root, _maxFileBytes);
    }

    public async Task<StoredReport> SaveAsync(Guid reportId, string fileName, string content, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetByteCount(content);
        if (bytes > _maxFileBytes)
        {
            throw new InvalidOperationException($"Report content is {bytes} bytes, which exceeds the {StorageOptions.SectionName}:{nameof(StorageOptions.MaxFileBytes)} limit of {_maxFileBytes} bytes.");
        }

        var extension = Path.GetExtension(SanitizeFileName(fileName)).TrimStart('.').ToLowerInvariant();
        if (extension.Length == 0) extension = "csv";

        var now = DateTimeOffset.UtcNow;
        var storageKey = $"{now:yyyy}/{now:MM}/{reportId}.{extension}";
        var path = ResolvePath(storageKey);

        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        // Atomic publish: write beside the target, then move into place.
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        _logger.LogInformation("Stored report {ReportId} at {StorageKey} ({SizeBytes} bytes)", reportId, storageKey, bytes);

        return new StoredReport(storageKey, SanitizeFileName(fileName), ResolveContentType(extension), bytes);
    }

    public async Task<string?> ReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var path = TryResolvePath(storageKey);
        if (path is null || !File.Exists(path)) return null;

        return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = TryResolvePath(storageKey);
        if (path is null || !File.Exists(path)) return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = TryResolvePath(storageKey);
        if (path is null || !File.Exists(path)) return Task.FromResult(false);

        File.Delete(path);
        _logger.LogInformation("Deleted stored report {StorageKey}", storageKey);
        return Task.FromResult(true);
    }

    /// <summary>Resolves a key to an absolute path, rejecting anything outside the root.</summary>
    private string ResolvePath(string storageKey)
    {
        var path = TryResolvePath(storageKey)
            ?? throw new ArgumentException($"Storage key '{storageKey}' is not a valid path within the storage root.", nameof(storageKey));

        return path;
    }

    private string? TryResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;

        var candidate = Path.GetFullPath(Path.Combine(_root, storageKey));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal) ? candidate : null;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string((fileName ?? string.Empty)
            .Where(c => !invalid.Contains(c) && !Path.GetInvalidPathChars().Contains(c))
            .ToArray()).Trim();

        return clean.Length == 0 ? "report.csv" : clean;
    }

    private static string ResolveContentType(string extension) => extension switch
    {
        "csv" => "text/csv",
        "json" => "application/json",
        "txt" => "text/plain",
        _ => "application/octet-stream"
    };

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary file {Path}", path);
        }
    }
}
