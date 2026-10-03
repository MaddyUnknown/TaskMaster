using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskMaster.Demo.Consumer.Configs;
using TaskMaster.Demo.Consumer.Data;
using TaskMaster.Demo.Consumer.Domain;
using TaskMaster.Demo.Consumer.Reporting;
using TaskMaster.Demo.Consumer.Reports;
using TaskMaster.Demo.Consumer.Storage;
using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Demo.Consumer.Handlers;

public sealed class GenerateReportHandler : IJobHandler<ReportJobPayload>
{
    private readonly IReportRepository _reports;
    private readonly IReportFileStore _fileStore;
    private readonly ISyntheticReportGenerator _generator;
    private readonly IReportCsvFormatter _formatter;
    private readonly DemoConsumerOptions _options;
    private readonly ILogger<GenerateReportHandler> _logger;

    public GenerateReportHandler(
        IReportRepository reports,
        IReportFileStore fileStore,
        ISyntheticReportGenerator generator,
        IReportCsvFormatter formatter,
        IOptions<DemoConsumerOptions> options,
        ILogger<GenerateReportHandler> logger)
    {
        _reports = reports;
        _fileStore = fileStore;
        _generator = generator;
        _formatter = formatter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(ReportJobPayload payload)
    {
        Validate(payload);

        var row = await _reports.GetAsync(payload.SubmissionId, CancellationToken.None)
            ?? throw new InvalidOperationException(
                $"No demo report row exists for submission {payload.SubmissionId}. " +
                "Start TaskMaster.Demo.Web first so the schema and row exist.");

        if (row.Status is ReportProcessingStatus.Completed)
        {
            _logger.LogInformation("Report {ReportId} is already completed; nothing to do.", payload.SubmissionId);
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Generating report '{Title}' with {RecordCount} records (report {ReportId})",
            row.Title, row.RecordCount, row.Id);

        await _reports.MarkRunningAsync(row.Id, DateTimeOffset.UtcNow, CancellationToken.None);

        try
        {
            var report = _generator.Generate(payload, TimeSpan.FromSeconds(_options.MaxExecutionSeconds));
            var formatted = _formatter.Format(report, _options.MaxResultBytes);

            var stored = await _fileStore.SaveAsync(
                row.Id, $"report-{row.Id:N}.csv", formatted.Content, CancellationToken.None);

            await _reports.MarkCompletedAsync(
                row.Id, stored, report.Rows.Count, formatted.Truncated, DateTimeOffset.UtcNow, CancellationToken.None);

            stopwatch.Stop();
            _logger.LogInformation("Report {ReportId} completed in {ElapsedMs} ms ({Rows} rows, truncated={Truncated})",
                row.Id, stopwatch.ElapsedMilliseconds, report.Rows.Count, formatted.Truncated);
        }
        catch (Exception ex)
        {
            try
            {
                await _reports.MarkFailedAsync(row.Id, ex.Message, DateTimeOffset.UtcNow, CancellationToken.None);
            }
            catch (Exception updateException)
            {
                _logger.LogError(updateException, "Could not mark report {ReportId} as failed", row.Id);
            }

            throw;
        }
    }

    private void Validate(ReportJobPayload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (payload.SubmissionId == Guid.Empty)
        {
            throw new ArgumentException("Payload SubmissionId is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.Title))
        {
            throw new ArgumentException("Payload Title is required.");
        }

        if (payload.RecordCount < 1 || payload.RecordCount > _options.MaxRecords)
        {
            throw new ArgumentException($"Payload RecordCount must be between 1 and {_options.MaxRecords}.");
        }
    }
}
