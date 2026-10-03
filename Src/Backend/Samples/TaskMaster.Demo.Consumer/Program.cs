using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TaskMaster.Demo.Consumer.BackgroundServices;
using TaskMaster.Demo.Consumer.Configs;
using TaskMaster.Demo.Consumer.Data;
using TaskMaster.Demo.Consumer.Domain;
using TaskMaster.Demo.Consumer.Handlers;
using TaskMaster.Demo.Consumer.Reporting;
using TaskMaster.Demo.Consumer.Reports;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.DependencyInjection;
using TaskMaster.Library.Consumer.Interfaces;

namespace TaskMaster.Demo.Consumer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("TaskMaster Demo Consumer");
        Console.WriteLine("=======================\n");

        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddUserSecrets<DemoDbContext>();

        var demoOptions = builder.Configuration
            .GetSection(DemoConsumerOptions.SectionName)
            .Get<DemoConsumerOptions>() ?? new DemoConsumerOptions();
        demoOptions.Validate();
        builder.Services.AddSingleton(demoOptions);

        // Existing Consumer SDK: worker lifecycle, polling, concurrency, heartbeats and
        // batched status reporting are provided by the SDK. The handler only has to
        // generate, store and record — no HTTP call to the web tier.
        var apiBaseUrl = builder.Configuration["TaskMaster:ApiBaseUrl"];
        builder.Services.AddTaskMasterConsumer(options =>
        {
            if (!string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                options.ApiBaseUrl = apiBaseUrl;
            }

            var clientId = builder.Configuration["TaskMaster:Oidc:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                options.Auth.Oidc = new TaskMasterConsumerOidcAuthOptions
                {
                    ClientId = clientId,
                    ClientSecret = builder.Configuration["TaskMaster:Oidc:ClientSecret"] ?? string.Empty
                };
            }

            options.MaxConcurrentHandlers = builder.Configuration.GetValue<int?>("Demo:MaxConcurrentHandlers") ?? 4;
        });

        builder.Services.Configure<DemoConsumerOptions>(
            builder.Configuration.GetSection(DemoConsumerOptions.SectionName));

        // Demo database: one row per report, written by the web tier on submit and by
        // this process on completion. The schema is created by Demo.Web, not here.
        builder.Services.AddDemoPersistence(builder.Configuration);
        builder.Services.AddScoped<IReportRepository, EfReportRepository>();

        // Report files. RootPath must match Demo.Web — this process writes, the web
        // tier serves.
        builder.Services.AddDemoStorage(builder.Configuration);

        // Fail rows abandoned by a crashed run instead of leaving the UI polling.
        builder.Services.AddHostedService<StaleReportReconciler>();

        builder.Services.AddSingleton<ISyntheticReportGenerator>(_ =>
        {
            var delayMs = Math.Max(0, demoOptions.RowDelayMilliseconds);
            return new SyntheticReportGenerator(delayMs > 0 ? () => Thread.Sleep(delayMs) : null);
        });
        builder.Services.AddSingleton<IReportCsvFormatter, ReportCsvFormatter>();

        using var host = builder.Build();

        var factory = host.Services.GetRequiredService<IWorkerFactory>();
        var workerName = host.Services.GetRequiredService<IOptions<DemoConsumerOptions>>().Value.WorkerName;

        var worker = factory.CreateWorker(workerName, configuration =>
        {
            configuration.Handle<ReportJobPayload, GenerateReportHandler>();
        });

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Console.WriteLine("\nShutting down…");
            cts.Cancel();
        };

        try
        {
            await worker.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"\nError: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
