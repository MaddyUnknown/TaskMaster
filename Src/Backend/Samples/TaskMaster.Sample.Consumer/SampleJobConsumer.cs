using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Sample.Consumer;

public class SampleJobConsumer
{
    private readonly IWorkerFactory _factory;

    public SampleJobConsumer(IServiceProvider serviceProvider)
    {
        _factory = serviceProvider.GetRequiredService<IWorkerFactory>();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var worker = _factory.CreateWorker("sample-consumer", cfg =>
        {
            cfg.Handle<SamplePayload, SampleHandler>();
        });

        try
        {
            await worker.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"\nError: {ex.Message}");
            Console.Error.WriteLine("Make sure the TaskMaster API is running and reachable.");
        }
    }
}
