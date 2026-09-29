using TaskMaster.Demo.Web.Configs;

namespace TaskMaster.Demo.Web.Domain;

public sealed class ReportRequest
{
    public string Title { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

public sealed class ReportRequestValidator
{
    private readonly DemoOptions _options;

    public ReportRequestValidator(DemoOptions options)
    {
        _options = options;
    }

    public IReadOnlyList<string> Validate(ReportRequest request)
    {
        var errors = new List<string>();

        if (request is null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0) errors.Add("Title is required.");
        if (title.Length > _options.MaxTitleLength) errors.Add($"Title must be at most {_options.MaxTitleLength} characters.");

        if (request.RecordCount < _options.MinRecordCount) errors.Add($"RecordCount must be at least {_options.MinRecordCount}.");
        if (request.RecordCount > _options.MaxRecordCount) errors.Add($"RecordCount must be at most {_options.MaxRecordCount}.");

        return errors;
    }
}
