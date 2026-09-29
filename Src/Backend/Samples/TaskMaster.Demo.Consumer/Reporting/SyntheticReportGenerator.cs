using System.Diagnostics;
using TaskMaster.Demo.Consumer.Domain;

namespace TaskMaster.Demo.Consumer.Reporting;

public sealed record ReportRow(
    DateTimeOffset TimestampUtc,
    string Region,
    string Product,
    int Units,
    decimal UnitPrice,
    decimal Revenue);

public sealed record RegionTotal(string Region, int Orders, long Units, decimal Revenue);

public sealed record ReportSummary(
    int RowCount,
    long TotalUnits,
    decimal TotalRevenue,
    IReadOnlyList<RegionTotal> RegionTotals);

public sealed record SyntheticReport(IReadOnlyList<ReportRow> Rows, ReportSummary Summary);

public interface ISyntheticReportGenerator
{
    /// <summary>
    /// Generates a deterministic synthetic sales report. Throws
    /// <see cref="TimeoutException"/> when the execution budget is exceeded.
    /// </summary>
    SyntheticReport Generate(ReportJobPayload payload, TimeSpan executionBudget);
}

/// <summary>
/// Generates fully synthetic sales rows (no external side effects, no I/O).
/// The same submission id always produces the same data, which keeps demos
/// reproducible and tests simple.
/// </summary>
public sealed class SyntheticReportGenerator : ISyntheticReportGenerator
{
    public static readonly string[] Regions = ["North", "South", "East", "West", "Central"];
    public static readonly string[] Products = ["Widget", "Gadget", "Doohickey", "Gizmo", "Contraption"];

    private readonly Action? _perRowDelay;

    public SyntheticReportGenerator(Action? perRowDelay = null)
    {
        _perRowDelay = perRowDelay;
    }

    public SyntheticReport Generate(ReportJobPayload payload, TimeSpan executionBudget)
    {
        if (payload.RecordCount < 1) throw new ArgumentException("RecordCount must be positive.", nameof(payload));

        var deadline = Stopwatch.GetTimestamp() + (long)(executionBudget.TotalSeconds * Stopwatch.Frequency);
        var random = new Random(GetStableSeed(payload.SubmissionId));

        var rows = new List<ReportRow>(payload.RecordCount);
        var baseUtc = DateTimeOffset.UtcNow.AddHours(-24);

        // Truncate to whole seconds so repeated generations stay byte-identical.
        var startUtc = new DateTimeOffset(baseUtc.Ticks - (baseUtc.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);

        for (var i = 0; i < payload.RecordCount; i++)
        {
            if (Stopwatch.GetTimestamp() > deadline)
            {
                throw new TimeoutException(
                    $"Report generation exceeded the execution budget of {executionBudget.TotalSeconds:0} seconds.");
            }

            var region = Regions[random.Next(Regions.Length)];
            var product = Products[random.Next(Products.Length)];
            var units = random.Next(1, 50);
            var unitPrice = Math.Round(random.Next(500, 50_000) / 100m, 2);
            var timestamp = startUtc.AddSeconds(random.Next(0, 24 * 3600));

            rows.Add(new ReportRow(timestamp, region, product, units, unitPrice, Math.Round(units * unitPrice, 2)));

            _perRowDelay?.Invoke();
        }

        return new SyntheticReport(rows, BuildSummary(rows));
    }

    internal static ReportSummary BuildSummary(IReadOnlyList<ReportRow> rows)
    {
        var totals = rows
            .GroupBy(r => r.Region, StringComparer.Ordinal)
            .Select(g => new RegionTotal(g.Key, g.Count(), g.Sum(r => r.Units), Math.Round(g.Sum(r => r.Revenue), 2)))
            .OrderByDescending(t => t.Revenue)
            .ToArray();

        return new ReportSummary(
            rows.Count,
            rows.Sum(r => r.Units),
            Math.Round(rows.Sum(r => r.Revenue), 2),
            totals);
    }

    private static int GetStableSeed(Guid submissionId)
    {
        var bytes = submissionId.ToByteArray();
        return BitConverter.ToInt32(bytes, 0) ^ BitConverter.ToInt32(bytes, 4);
    }
}
