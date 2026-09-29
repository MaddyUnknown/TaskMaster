namespace TaskMaster.Demo.Web.Data;

/// <summary>
/// Processing state of a demo report. Mirrors the TaskMaster job lifecycle without
/// introducing a second state machine: the consumer is the only writer of
/// <see cref="Running"/>, <see cref="Completed"/> and <see cref="Failed"/>, and the
/// web tier only ever creates a <see cref="Queued"/> row or deletes an expired one.
/// </summary>
public enum ReportProcessingStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}
