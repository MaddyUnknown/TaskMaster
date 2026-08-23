using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Producer.Configs;
using TaskMaster.Library.Producer.DependencyInjection;
using TaskMaster.Sample.Producer;

Console.WriteLine("TaskMaster Sample Producer");
Console.WriteLine("=========================\n");

var apiBaseUrl = args.Length > 0 ? args[0] : "https://localhost:7143";
var clientId = Environment.GetEnvironmentVariable("TASKMASTER_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("TASKMASTER_CLIENT_SECRET");

Console.WriteLine($"API Base URL: {apiBaseUrl}\n");

var services = new ServiceCollection();
services.AddTaskMasterProducer(opts =>
{
    opts.ApiBaseUrl = apiBaseUrl;

    if (!string.IsNullOrWhiteSpace(clientId))
    {
        opts.Auth.Oidc = new TaskMasterProducerOidcAuthOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret ?? string.Empty
        };
    }
});
await using var serviceProvider = services.BuildServiceProvider();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) =>
{
    Console.WriteLine("\nShutting down...");
    cts.Cancel();
};

var producer = new SampleJobProducer(serviceProvider);
await producer.RunAsync(cts.Token);

Console.WriteLine($"\nTotal messages produced: {producer.TotalProduced}");
