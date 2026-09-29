using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskMaster.Demo.Web.Configs;
using TaskMaster.Demo.Web.Data;
using TaskMaster.Demo.Web.Domain;
using TaskMaster.Demo.Web.Reports;
using TaskMaster.Demo.Web.Storage;
using TaskMaster.Library.Producer.Interfaces;

namespace TaskMaster.Demo.Web.Controllers;

/// <summary>
/// BFF endpoints for the demo UI. This tier owns two things only: creating the report
/// row and handing the job to the Producer SDK. It never learns anything from the
/// consumer over HTTP — progress is read straight from the shared row, so a demo
/// worker restart can never leave the UI polling a dead endpoint.
/// </summary>
[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly IProducer _producer;
    private readonly ReportRequestValidator _validator;
    private readonly IReportRepository _reports;
    private readonly IReportFileStore _fileStore;
    private readonly DemoOptions _options;

    public DemoController(
        IProducer producer,
        ReportRequestValidator validator,
        IReportRepository reports,
        IReportFileStore fileStore,
        DemoOptions options)
    {
        _producer = producer;
        _validator = validator;
        _reports = reports;
        _fileStore = fileStore;
        _options = options;
    }

    /// <summary>
    /// Creates the report row, then submits the TaskMaster job. The row is written
    /// first so the consumer always has a target to update; if the Producer SDK fails
    /// the row is marked Failed instead of being left dangling in Queued.
    /// </summary>
    [HttpPost("reports")]
    [EnableRateLimiting("submit")]
    public async Task<IResult> SubmitReport([FromBody] ReportRequest request, CancellationToken cancellationToken)
    {
        var errors = _validator.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = errors.ToArray() });
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new ReportRequestEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            RecordCount = request.RecordCount,
            SubmittedBy = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Status = ReportProcessingStatus.Queued,
            SubmittedAtUtc = now,
            ModifiedAtUtc = now
        };

        await _reports.AddAsync(entity, cancellationToken);

        try
        {
            await _producer.ProduceAsync(new ReportJobPayload
            {
                // The row id travels in the payload, which is how the consumer finds it.
                SubmissionId = entity.Id,
                Title = entity.Title,
                RecordCount = entity.RecordCount
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _reports.MarkFailedAsync(entity.Id, $"Task job submission failed: {ex.Message}", DateTimeOffset.UtcNow, cancellationToken);

            return Results.Json(
                new { error = "Task submission failed. Is the TaskMaster API running and reachable?" },
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Accepted($"/api/demo/reports/{entity.Id}", new
        {
            reportId = entity.Id,
            jobType = new { name = ReportJobPayload.JobTypeName, version = ReportJobPayload.JobTypeVersion }
        });
    }

    /// <summary>Polled by the UI until the status is Completed or Failed.</summary>
    [HttpGet("reports/{reportId:guid}")]
    public async Task<IResult> GetReport(Guid reportId, CancellationToken cancellationToken)
    {
        var row = await _reports.GetAsync(reportId, cancellationToken);
        if (row is null)
        {
            return Results.NotFound(new { error = "Report not found or expired." });
        }

        return Results.Ok(new
        {
            reportId = row.Id,
            title = row.Title,
            recordCount = row.RecordCount,
            status = row.Status.ToString().ToLowerInvariant(),
            submittedAt = row.SubmittedAtUtc,
            startedAt = row.StartedAtUtc,
            completedAt = row.CompletedAtUtc,
            modifiedAt = row.ModifiedAtUtc,
            rowCount = row.RowCount,
            fileName = row.FileName,
            sizeBytes = row.SizeBytes,
            truncated = row.Truncated,
            failureReason = row.FailureReason,
            downloadUrl = row.Status == ReportProcessingStatus.Completed && !string.IsNullOrWhiteSpace(row.StorageKey)
                ? $"/api/demo/reports/{row.Id}/download"
                : null
        });
    }

    /// <summary>Preview of the generated CSV, capped for the browser.</summary>
    [HttpGet("reports/{reportId:guid}/result")]
    public async Task<IResult> GetReportResult(Guid reportId, CancellationToken cancellationToken)
    {
        var row = await _reports.GetAsync(reportId, cancellationToken);
        if (row is null)
        {
            return Results.NotFound(new { error = "Report not found or expired." });
        }

        if (row.Status != ReportProcessingStatus.Completed || string.IsNullOrWhiteSpace(row.StorageKey))
        {
            return Results.Conflict(new { error = "Report is not ready yet.", status = row.Status.ToString().ToLowerInvariant() });
        }

        var content = await _fileStore.ReadAsync(row.StorageKey, cancellationToken);
        if (content is null)
        {
            return Results.NotFound(new { error = "Report file is no longer available." });
        }

        return Results.Ok(new
        {
            reportId = row.Id,
            fileName = row.FileName,
            contentType = row.ContentType ?? "text/csv",
            sizeBytes = row.SizeBytes,
            rowCount = row.RowCount,
            truncated = row.Truncated,
            generatedAt = row.CompletedAtUtc,
            content = CapPreview(content)
        });
    }

    /// <summary>Streams the stored file as an attachment. Not capped — this is the real file.</summary>
    [HttpGet("reports/{reportId:guid}/download")]
    public async Task<IResult> DownloadReport(Guid reportId, CancellationToken cancellationToken)
    {
        var row = await _reports.GetAsync(reportId, cancellationToken);
        if (row is null)
        {
            return Results.NotFound(new { error = "Report not found or expired." });
        }

        if (row.Status != ReportProcessingStatus.Completed || string.IsNullOrWhiteSpace(row.StorageKey))
        {
            return Results.Conflict(new { error = "Report is not ready yet.", status = row.Status.ToString().ToLowerInvariant() });
        }

        var stream = await _fileStore.OpenReadAsync(row.StorageKey, cancellationToken);
        if (stream is null)
        {
            return Results.NotFound(new { error = "Report file is no longer available." });
        }

        return Results.File(stream, row.ContentType ?? "text/csv", row.FileName ?? $"report-{row.Id}.csv");
    }

    private string CapPreview(string content)
    {
        var limit = Math.Max(1024, _options.ResultMaxBytes);
        return Encoding.UTF8.GetByteCount(content) <= limit
            ? content
            : TruncateToBytes(content, limit);
    }

    private static string TruncateToBytes(string content, int maxBytes)
    {
        // Respect UTF-8 boundaries so the preview never ends mid-character.
        var limit = Math.Min(content.Length, maxBytes);
        while (limit > 0 && Encoding.UTF8.GetByteCount(content.AsSpan(0, limit)) > maxBytes)
        {
            limit--;
        }

        return content[..limit];
    }
}
