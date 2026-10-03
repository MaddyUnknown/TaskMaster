using TaskMaster.Demo.Web.Storage;

namespace TaskMaster.Demo.Web;

public static class DemoStorageServiceCollectionExtensions
{
    public static IServiceCollection AddDemoStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

        if (!string.Equals(options.Provider, StorageOptions.LocalProvider, StringComparison.OrdinalIgnoreCase))
        {
            // Surfaced now, not on the first job.
            options.Validate();
        }

        services.AddSingleton(options);
        services.AddSingleton<IReportFileStore, LocalFileReportStore>();
        return services;
    }
}
