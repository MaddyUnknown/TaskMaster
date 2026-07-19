using TaskMaster.Library.Consumer.Attributes;

namespace TaskMaster.Sample.Consumer;

[JobType("sample-message", 1)]
public sealed record SamplePayload(string Message, DateTime ProducedAt, int SequenceNumber);
