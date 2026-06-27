using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using TaskMaster.Library.Common.Caches;
using TaskMaster.Library.Common.Interfaces.Caches;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Registries;

namespace TaskMaster.Test.UnitTests.CommonTests;

public class JobTypeSchemaRegistryTests
{
    private Mock<IApiHttpClient> _httpClient = null!;
    private ICache _cache = null!;

    private JobTypeSchemaRegistry CreateRegistry(int ttl = 5) =>
        new(_httpClient.Object, _cache, ttl);

    [SetUp]
    public void SetupMock()
    {
        _httpClient = new(MockBehavior.Strict);
        var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _cache = new InMemoryCache(memoryCache, Options.Create(new MemoryCacheOptions()));
    }

    [TearDown]
    public void TearDown()
    {
        if (_cache is IDisposable d) d.Dispose();
    }

    [Test]
    public async Task GetByJobTypeNameAndVersion_WhenFound_ShouldReturnSchema()
    {
        _httpClient
            .Setup(c => c.GetJobType(It.IsAny<GetJobTypeRequest>()))
            .ReturnsAsync(new JobTypeDetails { Name = "email", Version = 1, Schema = "{}" });

        var result = await CreateRegistry().GetByJobTypeNameAndVersion("email", 1);

        Assert.That(result, Is.EqualTo("{}"));
    }

    [Test]
    public void GetByJobTypeNameAndVersion_WhenNotFound_ShouldThrow()
    {
        _httpClient
            .Setup(c => c.GetJobType(It.IsAny<GetJobTypeRequest>()))
            .ReturnsAsync((JobTypeDetails?)null);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateRegistry().GetByJobTypeNameAndVersion("missing", 1));
    }
}
