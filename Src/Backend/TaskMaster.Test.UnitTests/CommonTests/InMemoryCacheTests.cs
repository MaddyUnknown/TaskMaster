using Microsoft.Extensions.DependencyInjection;
using TaskMaster.Library.Common.Caches;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.DependencyInjection;
using TaskMaster.Library.Common.Interfaces.Caches;

namespace TaskMaster.Test.UnitTests.CommonTests;

public class InMemoryCacheTests
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddTaskMasterCommon(new ApiConfig { ApiBaseUrl = "https://test/" });
        return services.BuildServiceProvider();
    }

    [Test]
    public void GetOrAdd_WhenKeyNotCached_ShouldCallFactory()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<ICache>();

        var callCount = 0;
        var result = cache.GetOrAdd("key", () => { callCount++; return "value"; });

        Assert.That(result, Is.EqualTo("value"));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void GetOrAdd_WhenKeyIsCached_ShouldReturnCachedValue()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<ICache>();

        cache.GetOrAdd("key", () => "original");
        var callCount = 0;
        var result = cache.GetOrAdd("key", () => { callCount++; return "new"; });

        Assert.That(result, Is.EqualTo("original"));
        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetOrAddAsync_WhenKeyNotCached_ShouldCallFactory()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<ICache>();

        var callCount = 0;
        var result = await cache.GetOrAddAsync("key", async () => { callCount++; await Task.CompletedTask; return "value"; });

        Assert.That(result, Is.EqualTo("value"));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetOrAddAsync_WhenKeyIsCached_ShouldReturnCachedValue()
    {
        using var sp = BuildProvider();
        var cache = sp.GetRequiredService<ICache>();

        await cache.GetOrAddAsync("key", () => Task.FromResult("original"));
        var callCount = 0;
        var result = await cache.GetOrAddAsync("key", async () => { callCount++; await Task.CompletedTask; return "new"; });

        Assert.That(result, Is.EqualTo("original"));
        Assert.That(callCount, Is.EqualTo(0));
    }
}
