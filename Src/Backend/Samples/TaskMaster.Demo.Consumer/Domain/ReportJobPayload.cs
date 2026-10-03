using TaskMaster.Demo.Consumer.Domain;
using TaskMaster.Library.Consumer.Attributes;

namespace TaskMaster.Demo.Consumer.Domain;

[JobType(ReportJobPayload.JobTypeName, ReportJobPayload.JobTypeVersion)]
public sealed class ReportJobPayload
{
    public const string JobTypeName = "demo-report";
    public const long JobTypeVersion = 1;

    public Guid SubmissionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int RecordCount { get; set; }
}
