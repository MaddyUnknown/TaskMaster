using TaskMaster.API.Entities;
using TaskMaster.API.Enums;

namespace TaskMaster.Test.IntegrationTests.Data;

internal static class TestData
{
    public static JobType JobType(string name = "email", long version = 1) => new()
    {
        Name = name,
        Version = version,
        Schema = "{}"
    };

    public static Worker Worker(string name, IEnumerable<JobType>? jobTypes = null) => new()
    {
        WorkerPublicId = Guid.NewGuid(),
        WorkerName = name,
        Status = WorkerStatusEnum.Active,
        WorkerCapabilities = jobTypes == null ? [] : jobTypes.Select(jt => new WorkerCapability { JobType = jt }).ToArray(),
        AssignedJobs = []
    };

    public static Job Job(JobType jobType, string payload = "{\"id\":1}") => new()
    {
        JobPublicId = Guid.NewGuid(),
        JobType = jobType,
        Payload = payload,
        Status = JobStatusEnum.Queued
    };
}
