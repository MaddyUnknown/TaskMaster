using System.Globalization;
using System.Text;

namespace TaskMaster.Demo.Consumer.Reporting;

public sealed record FormattedReport(string Content, int RowCount, bool Truncated);

public interface IReportCsvFormatter
{
    /// <summary>Formats a report as CSV, constrained to <paramref name="maxBytes"/> UTF-8 bytes.</summary>
    FormattedReport Format(SyntheticReport report, long maxBytes);
}

public sealed class ReportCsvFormatter : IReportCsvFormatter
{
    private const string Header = "TimestampUtc,Region,Product,Units,UnitPrice,Revenue";
    private const string TruncationMarker = "# truncated to respect the configured result size limit";

    public FormattedReport Format(SyntheticReport report, long maxBytes)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Header);

        foreach (var row in report.Rows)
        {
            builder.Append(row.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Region).Append(',')
                .Append(row.Product).Append(',')
                .Append(row.Units.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                .AppendLine(row.Revenue.ToString("0.00", CultureInfo.InvariantCulture));
        }

        AppendSummary(builder, report.Summary);

        if (SizeOf(builder) <= maxBytes)
        {
            return new FormattedReport(builder.ToString(), report.Rows.Count, Truncated: false);
        }

        // Drop summary first, then truncate rows until the content fits.
        builder.Clear();
        builder.AppendLine(Header);

        foreach (var row in report.Rows)
        {
            var line = FormatRow(row);
            if (SizeOf(builder) + SizeOf(line) > maxBytes - SizeOf(TruncationMarker) - 2)
            {
                break;
            }

            builder.Append(line);
        }

        builder.Append(TruncationMarker).AppendLine();
        return new FormattedReport(builder.ToString(), report.Rows.Count, Truncated: true);
    }

    private static void AppendSummary(StringBuilder builder, ReportSummary summary)
    {
        builder.Append('\n');
        builder.Append(CultureInfo.InvariantCulture, $"# rows,{summary.RowCount}\n");
        builder.Append(CultureInfo.InvariantCulture, $"# totalUnits,{summary.TotalUnits}\n");
        builder.Append(CultureInfo.InvariantCulture, $"# totalRevenue,{summary.TotalRevenue:0.00}\n");

        foreach (var region in summary.RegionTotals)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"# region,{region.Region},{region.Orders},{region.Units},{region.Revenue:0.00}\n");
        }
    }

    private static string FormatRow(ReportRow row) => string.Create(CultureInfo.InvariantCulture,
        $"{row.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)},{row.Region},{row.Product},{row.Units},{row.UnitPrice:0.00},{row.Revenue:0.00}\n");

    private static long SizeOf(StringBuilder builder) => Encoding.UTF8.GetByteCount(builder.ToString());
    private static long SizeOf(string value) => Encoding.UTF8.GetByteCount(value);
}
