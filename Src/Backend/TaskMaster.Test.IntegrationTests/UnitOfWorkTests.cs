using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Entities;
using TaskMaster.Test.IntegrationTests.Abstracts;
using TaskMaster.Test.IntegrationTests.Providers;
using TaskMaster.Test.IntegrationTests.Data;

namespace TaskMaster.Test.IntegrationTests;

public class UnitOfWorkTests : ProviderIntegrationTestBase
{

    [Test]
    public async Task BeginAndCommit_ShouldPersistChanges()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Act
        await uow.BeginTransactionAsync();
        db.Add(new JobType { Name = "new", Version = 2, Schema = "{}" });
        await uow.SaveAsync();
        await uow.CommitTransactionAsync();

        // Assert
        await using var assertScope = ServiceProvider.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var types = await assertDb.JobTypes.ToListAsync();
        Assert.That(types, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task BeginAndRollback_ShouldNotPersistChanges()
    {
        // Arrange
        await ExecuteDbAsync(async db =>
        {
            var jobType = TestData.JobType();
            db.Add(jobType);
            await db.SaveChangesAsync();
        });

        await using var scope = ServiceProvider.CreateAsyncScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Act
        await uow.BeginTransactionAsync();
        db.Add(new JobType { Name = "rollback", Version = 99, Schema = "{}" });
        await uow.RollbackTransactionAsync();

        // Assert
        await using var assertScope = ServiceProvider.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var types = await assertDb.JobTypes.ToListAsync();
        Assert.That(types, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AuditInterceptor_ShouldSetTimestampsOnCreate()
    {
        // Arrange
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Act
        db.Add(TestData.JobType());
        await uow.SaveAsync();

        // Assert
        await using var assertScope = ServiceProvider.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var saved = await assertDb.JobTypes.SingleAsync();
        Assert.That(saved.CreatedDateTime, Is.Not.EqualTo(default));
    }
}
