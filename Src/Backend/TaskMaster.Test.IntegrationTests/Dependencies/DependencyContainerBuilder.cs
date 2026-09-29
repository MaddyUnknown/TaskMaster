using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;
using TaskMaster.API.DependencyInjection;
using TaskMaster.Test.IntegrationTests.Providers;

namespace TaskMaster.Test.IntegrationTests.Dependencies
{
    public static class DependencyContainerBuilder
    {
        public static IConfigurationRoot GetConfiguration() =>
           new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.testing.json", true, true)
               .AddUserSecrets<ConcurrencyTests>(optional: true)
               .AddEnvironmentVariables()
               .Build();

        /// <summary>
        /// Builds a host that mirrors the production registration path
        /// (<c>AddTaskMasterPersistence</c> + <c>AddTaskMasterApplication</c>) so integration
        /// tests exercise the same wiring the API runs with. The engine and its connection
        /// string are layered over the test configuration.
        /// </summary>
        public static ServiceProvider GetServicesProvider(ITestProvider provider, IConfigurationRoot baseConfiguration)
        {
            var connectionString = provider.ResolveConnectionString(baseConfiguration)
                ?? throw new InvalidOperationException(
                    $"Connection string 'ConnectionStrings:{provider.ConnectionStringName}' was not configured.");

            var configuration = new ConfigurationBuilder()
                .AddConfiguration(baseConfiguration)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Provider"] = provider.Provider.ToString(),
                    [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.DefaultConnectionStringName}"] = connectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(configuration);

            services.AddTaskMasterPersistence(configuration);
            services.AddTaskMasterApplication(configuration);

            services.AddSingleton(provider);
            services.AddScoped<Factories.DatabaseFixture>();

            return services.BuildServiceProvider();
        }
    }
}
