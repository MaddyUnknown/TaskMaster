namespace TaskMaster.Demo.Consumer.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage backend. Only <c>Local</c> is implemented. <c>S3</c> and <c>AzureBlob</c>
    /// are the intended targets of the <see cref="IReportFileStore"/> seam; selecting
    /// one fails fast rather than silently falling back.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Root directory for the <c>Local</c> provider. Must be identical in
    /// TaskMaster.Demo.Web and TaskMaster.Demo.Consumer, because the consumer writes the
    /// file and the web tier serves it.
    /// </summary>
    public string RootPath { get; set; } = "./storage/reports";

    /// <summary>Hard ceiling on a single stored file. Larger content is rejected rather than written.</summary>
    public long MaxFileBytes { get; set; } = 8L * 1024 * 1024;

    public const string LocalProvider = "Local";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RootPath))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(RootPath)} is required for the '{Provider}' provider.");
        }

        if (!string.Equals(Provider, LocalProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Storage provider '{Provider}' is not implemented. The demo ships '{LocalProvider}' only — " +
                "add an IReportFileStore implementation for your object store and register it here.");
        }

        if (MaxFileBytes < 1024)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(MaxFileBytes)} must be at least 1024 bytes.");
        }
    }
}
