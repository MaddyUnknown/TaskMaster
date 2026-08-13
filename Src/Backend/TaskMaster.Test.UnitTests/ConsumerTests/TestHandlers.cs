using TaskMaster.Library.Consumer.Attributes;
using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Test.UnitTests.ConsumerTests;

[JobType("email", 1)]
internal sealed record EmailPayload(string Email, int Priority);

internal class EmailHandler : IJobHandler<EmailPayload>
{
    public Task HandleAsync(EmailPayload payload)
        => Task.CompletedTask;
}

internal class EmailCapturingHandler : IJobHandler<EmailPayload>
{
    public EmailPayload? ReceivedPayload { get; private set; }

    public Task HandleAsync(EmailPayload payload)
    {
        ReceivedPayload = payload;
        return Task.CompletedTask;
    }
}

internal class EmailFailingHandler : IJobHandler<EmailPayload>
{
    public Task HandleAsync(EmailPayload payload)
        => throw new InvalidOperationException("handler failed");
}

internal sealed record UnattributedPayload
{
    public string Name { get; set; } = string.Empty;
}

internal class UnattributedHandler : IJobHandler<UnattributedPayload>
{
    public Task HandleAsync(UnattributedPayload payload)
        => Task.CompletedTask;
}
