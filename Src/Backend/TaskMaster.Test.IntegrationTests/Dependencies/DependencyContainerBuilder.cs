using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;
using TaskMaster.API.DependencyInjection;
using TaskMaster.Test.IntegrationTests.Factories;
using TaskMaster.Test.IntegrationTests.Providers;

namespace TaskMaster.Test.IntegrationTests.Dependencies
{
    public static class DependencyContainerBuilder
    {
        public static IConfiguration GetConfiguration() =>
           new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", true, true)
               .AddUserSecrets<ConcurrencyTests>(optional: true)
               .AddEnvironmentVariables()
               .Build();

        public static ServiceProvider GetServicesProvider(ITestProvider provider, IConfiguration baseConfiguration)
        {
            var connectionString = provider.ResolveConnectionString(baseConfiguration)
                ?? throw new InvalidOperationException($"Connection string 'ConnectionStrings:{provider.ConnectionStringName}' was not configured.");

            var configuration = new ConfigurationBuilder()
                .AddConfiguration(baseConfiguration)
                // Used for 'TaskMaster.API' services 
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.DefaultConnectionStringName}"] = connectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(configuration);

            services.AddTaskMasterPersistence(configuration);
            services.AddTaskMasterApplication(configuration);

            services.AddSingleton(provider);
            services.AddScoped<DatabaseFixture>();

            return services.BuildServiceProvider();
        }
    }
}
