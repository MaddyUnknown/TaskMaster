using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Data;

namespace TaskMaster.IntegrationTests;

public abstract class IntegrationTestBase
{
    private ServiceProvider? _serviceProvider;
    private SqlServerFactory? _factory;

    protected IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("The integration test service provider has not been initialized");
    protected SqlServerFactory Factory => _factory ?? throw new InvalidOperationException("The integration test factory has not been initialized.");

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _serviceProvider = DependencyContainerBuilder.GetServicesProvider();
        _factory = _serviceProvider.GetService<SqlServerFactory>();
        
        if(_factory != null) await _factory.InitializeAsync();
    }

    [SetUp]
    public virtual async Task SetUpAsync() => await Factory.ResetDatabaseAsync();

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDownAsync()
    {
        if (_factory != null) await _factory.DisposeAsync();
        if(_serviceProvider != null) _serviceProvider.Dispose();
    }

    protected async Task<T> ExecuteDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }

    protected async Task ExecuteDbAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(db);
    }
}
