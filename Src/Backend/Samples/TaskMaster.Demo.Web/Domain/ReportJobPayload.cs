using TaskMaster.Library.Producer.Attributes;

namespace TaskMaster.Demo.Web.Domain;

[JobType(ReportJobPayload.JobTypeName, ReportJobPayload.JobTypeVersion)]
public sealed class ReportJobPayload
{
    public const string JobTypeName = "demo-report";
    public const long JobTypeVersion = 1;

    public Guid SubmissionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int RecordCount { get; set; }

    public const string SchemaJson = """
    {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "SubmissionId": { "type": "string", "minLength": 1 },
        "Title": { "type": "string" },
        "RecordCount": { "type": "integer", "minimum": 1 }
      },
      "required": ["SubmissionId", "Title", "RecordCount"]
    }
    """;
}
