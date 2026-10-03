namespace TaskMaster.Demo.Web.Configs;

public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public int DefaultRecordCount { get; set; } = 2000;
    public int MinRecordCount { get; set; } = 10;
    public int MaxRecordCount { get; set; } = 10_000;
    public int MaxTitleLength { get; set; } = 80;

    public int MaxJobSubmissionsPerUserPerMinute { get; set; } = 5;

    // Upper bound on the CSV preview returned by the result endpoint. The download is not capped.
    public int ResultMaxBytes { get; set; } = 1_048_576;

    // How long a generated report and its row survive. The demo is a one-shot tool: a report is generated, downloaded, then discarded. Default 24 hours.
    public int ReportRetentionHours { get; set; } = 24;

    // How often the retention sweep runs.
    public int RetentionSweepMinutes { get; set; } = 15;
}
