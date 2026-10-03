namespace TaskMaster.Demo.Consumer.Storage;

public sealed record StoredReport(string StorageKey, string FileName, string ContentType, long SizeBytes);

public interface IReportFileStore
{
    Task<StoredReport> SaveAsync(Guid reportId, string fileName, string content, CancellationToken cancellationToken);

    Task<string?> ReadAsync(string storageKey, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
