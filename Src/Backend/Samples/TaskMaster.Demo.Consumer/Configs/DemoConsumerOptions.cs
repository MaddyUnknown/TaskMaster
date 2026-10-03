namespace TaskMaster.Demo.Consumer.Configs;

public sealed class DemoConsumerOptions
{
    public const string SectionName = "Demo";

    public int MaxRecords { get; set; } = 10_000;
    public long MaxResultBytes { get; set; } = 1024 * 1024;
    public int MaxExecutionSeconds { get; set; } = 120;

    public int RowDelayMilliseconds { get; set; } = 2;

    public string WorkerName { get; set; } = "demo-report-worker";

    public int StaleReportTimeoutMinutes { get; set; } = 10;

    public int StaleSweepMinutes { get; set; } = 5;

    public void Validate()
    {
        var errors = new List<string>();
        if (MaxRecords < 1) errors.Add($"{SectionName}:{nameof(MaxRecords)} must be positive.");
        if (MaxResultBytes < 256) errors.Add($"{SectionName}:{nameof(MaxResultBytes)} must be at least 256 bytes.");
        if (StaleReportTimeoutMinutes <= 0) errors.Add($"{SectionName}:{nameof(StaleReportTimeoutMinutes)} must be positive.");

        if (StaleReportTimeoutMinutes * 60 <= MaxExecutionSeconds)
        {
            errors.Add($"{SectionName}:{nameof(StaleReportTimeoutMinutes)} must exceed {nameof(MaxExecutionSeconds)} so a healthy long-running job is not failed underneath itself.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid demo consumer configuration: " + string.Join(" ", errors));
        }
    }
}
