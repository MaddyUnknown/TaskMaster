namespace TaskMaster.Demo.Consumer.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";

    public string RootPath { get; set; } = "./storage/reports";

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
