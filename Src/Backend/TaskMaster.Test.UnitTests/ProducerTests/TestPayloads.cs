using TaskMaster.Library.Producer.Attributes;

namespace TaskMaster.Test.UnitTests.ProducerTests;

[JobType("email", 1)]
internal sealed record EmailPayload(string Email, int Priority);
