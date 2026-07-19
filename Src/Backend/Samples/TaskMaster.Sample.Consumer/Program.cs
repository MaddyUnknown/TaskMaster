using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Consumer.DependencyInjection;
using TaskMaster.Sample.Consumer;

Console.WriteLine("TaskMaster Sample Consumer");
Console.WriteLine("=========================\n");

var apiBaseUrl = args.Length > 0 ? args[0] : "https://localhost:7143";
Console.WriteLine($"API Base URL: {apiBaseUrl}\n");

var services = new ServiceCollection();
services.AddTaskMasterConsumer(opts =>
{
    opts.ApiBaseUrl = apiBaseUrl;
    opts.PollingWaitIntervalMs = 500;
});
await using var serviceProvider = services.BuildServiceProvider();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

var consumer = new SampleJobConsumer(serviceProvider);
await consumer.RunAsync(cts.Token);

Console.WriteLine("\nConsumer stopped.");
