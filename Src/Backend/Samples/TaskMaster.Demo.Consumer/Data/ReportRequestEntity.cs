namespace TaskMaster.Demo.Consumer.Data;

/// <summary>
/// One row per generated report. <see cref="Id"/> doubles as the correlation id that
/// travels inside the TaskMaster job payload, so this process can find its row without
/// any HTTP call to the web tier.
///
/// TaskMaster.Demo.Web carries an identical copy of this entity. The two never load into
/// the same process, so the duplication is inert; the table name and column names are
/// pinned explicitly in <see cref="DemoDbContext"/> rather than left to EF naming
/// conventions, so the two models cannot drift apart silently.
/// </summary>
public sealed class ReportRequestEntity
{
    /// <summary>Correlation id. Equals the <c>SubmissionId</c> in the job payload.</summary>
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int RecordCount { get; set; }

    /// <summary>Free-form submitter marker (the remote IP in the demo).</summary>
    public string? SubmittedBy { get; set; }

    public ReportProcessingStatus Status { get; set; } = ReportProcessingStatus.Queued;

    /// <summary>Populated when <see cref="Status"/> is <see cref="ReportProcessingStatus.Failed"/>.</summary>
    public string? FailureReason { get; set; }

    public string? FileName { get; set; }

    /// <summary>
    /// Storage-agnostic key produced by <c>IReportFileStore</c>. A relative path today;
    /// an object-store key when the local provider is swapped out.
    /// </summary>
    public string? StorageKey { get; set; }

    public string? ContentType { get; set; }

    public int RowCount { get; set; }

    public bool Truncated { get; set; }

    public long SizeBytes { get; set; }

    /// <summary>Drives the retention cutoff. All timestamps are UTC.</summary>
    public DateTimeOffset SubmittedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset ModifiedAtUtc { get; set; }
}
