namespace TaskMaster.Demo.Web.Storage;

/// <summary>Result of persisting a generated report file.</summary>
public sealed record StoredReport(string StorageKey, string FileName, string ContentType, long SizeBytes);

/// <summary>
/// Storage seam for generated reports. The demo ships a local-filesystem
/// implementation; swapping in an object store (S3, Azure Blob) means supplying another
/// implementation of this interface and changing <c>Storage:Provider</c> — nothing in
/// the controller, the handler or the database schema changes, because only the
/// storage-agnostic <see cref="StoredReport.StorageKey"/> is ever persisted.
///
/// TaskMaster.Demo.Consumer holds an identical copy. Both processes must be configured
/// with the same <c>Storage:RootPath</c> (or, later, the same container).
/// </summary>
public interface IReportFileStore
{
    /// <summary>Persists the content and returns its storage key and size.</summary>
    Task<StoredReport> SaveAsync(Guid reportId, string fileName, string content, CancellationToken cancellationToken);

    /// <summary>Reads the whole content as text, or <c>null</c> when the key is unknown.</summary>
    Task<string?> ReadAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>Opens the content for streaming, or <c>null</c> when the key is unknown.</summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>Deletes the content. Returns <c>false</c> when the key was already gone.</summary>
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
