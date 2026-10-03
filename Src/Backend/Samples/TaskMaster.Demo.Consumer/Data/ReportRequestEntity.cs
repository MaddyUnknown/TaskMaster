namespace TaskMaster.Demo.Consumer.Data;

public sealed class ReportRequestEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int RecordCount { get; set; }

    public string? SubmittedBy { get; set; }

    public ReportProcessingStatus Status { get; set; } = ReportProcessingStatus.Queued;

    public string? FailureReason { get; set; }

    public string? FileName { get; set; }

    public string? StorageKey { get; set; }

    public string? ContentType { get; set; }

    public int RowCount { get; set; }

    public bool Truncated { get; set; }

    public long SizeBytes { get; set; }

    public DateTimeOffset SubmittedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset ModifiedAtUtc { get; set; }
}
