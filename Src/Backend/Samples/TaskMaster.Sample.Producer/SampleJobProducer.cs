using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Producer.Interfaces;

namespace TaskMaster.Sample.Producer;

public class SampleJobProducer
{
    private readonly IProducer _producer;
    private int _seq;

    public int TotalProduced => _seq;

    public SampleJobProducer(IServiceProvider serviceProvider)
    {
        _producer = serviceProvider.GetRequiredService<IProducer>();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var random = Random.Shared;

        while (!cancellationToken.IsCancellationRequested)
        {
            var isBurst = random.NextDouble() < 0.3;
            var count = isBurst ? random.Next(3, 11) : 1;

            for (var i = 0; i < count; i++)
            {
                var payload = new SamplePayload
                {
                    Message = $"Hello from sample producer #{_seq + 1}{(isBurst && count > 1 ? " (burst)" : "")}",
                    ProducedAt = DateTime.Now,
                    SequenceNumber = ++_seq
                };

                try
                {
                    await _producer.ProduceAsync(payload);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Produced #{_seq}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FAILED #{_seq}: {ex.Message}");
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                var delayMs = random.Next(10, 5001);
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }
}
