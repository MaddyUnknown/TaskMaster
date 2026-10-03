using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.Test.IntegrationTests.Dependencies;
using TaskMaster.Test.IntegrationTests.Factories;
using TaskMaster.Test.IntegrationTests.Providers;

namespace TaskMaster.Test.IntegrationTests.Abstracts;

public abstract class ProviderIntegrationTestBase
{
    private ITestProvider? _testProvider;
    private ServiceProvider? _serviceProvider;

    protected ITestProvider TestProvider => _testProvider ?? throw new InvalidOperationException("The test provider has not been resolved yet");

    protected IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("The integration test service provider has not been initialized");

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        var configuration = DependencyContainerBuilder.GetConfiguration();
        _testProvider = TestProviderFactory.CreateForCurrentRun(configuration);

        if (_testProvider.ResolveConnectionString(configuration) is null)
        {
            Assert.Ignore($"'{_testProvider.Name}' is not configured. Set ConnectionStrings:{_testProvider.ConnectionStringName} to run the suite against {_testProvider.Name}.");
            return;
        }

        _serviceProvider = DependencyContainerBuilder.GetServicesProvider(_testProvider, configuration);

        await using var scope = _serviceProvider.CreateAsyncScope();
        var fixture = scope.ServiceProvider.GetRequiredService<DatabaseFixture>();
        await fixture.InitializeAsync();
    }

    [SetUp]
    public virtual async Task SetUpAsync()
    {
        if (_serviceProvider is null) return;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var fixture = scope.ServiceProvider.GetRequiredService<DatabaseFixture>();
        await fixture.ResetDatabaseAsync();
    }

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDownAsync()
    {
        if (_serviceProvider is not null)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var fixture = scope.ServiceProvider.GetRequiredService<DatabaseFixture>();
            await fixture.DisposeAsync();
        }

        _serviceProvider?.Dispose();
        _serviceProvider = null;
    }

    protected async Task<T> ExecuteDbAsync<T>(Func<IApplicationDbContext, Task<T>> action)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        return await action(db);
    }

    protected async Task ExecuteDbAsync(Func<IApplicationDbContext, Task> action)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        await action(db);
    }

    protected async Task<T> ExecuteServiceAsync<T>(Func<IServiceProvider, T> resolve) where T : notnull
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        return resolve(scope.ServiceProvider);
    }
}
