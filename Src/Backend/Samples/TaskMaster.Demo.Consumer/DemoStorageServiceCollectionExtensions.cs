using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Demo.Consumer.Storage;

namespace TaskMaster.Demo.Consumer;

public static class DemoStorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configured report file store. Selecting an unimplemented provider
    /// throws here rather than failing on the first job.
    /// </summary>
    public static IServiceCollection AddDemoStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

        if (!string.Equals(options.Provider, StorageOptions.LocalProvider, StringComparison.OrdinalIgnoreCase))
        {
            // Surfaced now, not mid-job.
            options.Validate();
        }

        services.AddSingleton(options);
        services.AddSingleton<IReportFileStore, LocalFileReportStore>();
        return services;
    }
}
