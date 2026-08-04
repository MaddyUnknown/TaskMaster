using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Sample.Consumer;

public class SampleHandler : IJobHandler<SamplePayload>
{
    public Task HandleAsync(SamplePayload payload)
    {
        var delay = DateTime.Now - payload.ProducedAt;
        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] Received #{payload.SequenceNumber}: \"{payload.Message}\" " +
            $"| Delay: {delay.TotalMilliseconds:F1}ms");
        return Task.CompletedTask;
    }
}
