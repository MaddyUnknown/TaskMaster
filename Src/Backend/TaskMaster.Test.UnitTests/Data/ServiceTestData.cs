using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.Test.UnitTests.Data;

internal static class ServiceTestData
{
    public static JobTypeRef EmailJobTypeRef => new() { Name = "email", Version = 1 };
    public static JobTypeRef VideoJobTypeRef => new() { Name = "video", Version = 2 };
    public static string Emailv1Schema = """
        {
            "type": "object",
            "required": ["Email", "Priority"],
            "properties": {
            "Email": { "type": "string", "pattern": "@" },
            "Priority": { "type": "integer", "minimum": 1 }
            },
            "additionalProperties": false
        }
    """;

    public static JobType EmailJobType(long id = 11) => new()
    {
        Id = id,
        Name = EmailJobTypeRef.Name,
        Version = EmailJobTypeRef.Version,
        Schema = "{}"
    };

    public static JobType VideoJobType(long id = 12) => new()
    {
        Id = id,
        Name = VideoJobTypeRef.Name,
        Version = VideoJobTypeRef.Version,
        Schema = "{}"
    };

    public static Worker InactiveWorker(Guid? publicId = null, long id = 102, ICollection<JobType>? capabilities = null) => new()
    {
        Id = id,
        WorkerPublicId = publicId ?? Guid.NewGuid(),
        WorkerName = "worker-a",
        Status = WorkerStatusEnum.InActive,
        WorkerCapabilities = capabilities == null ? [] : capabilities.Select(c => new WorkerCapability { WorkerId = id, JobTypeId = c.Id, JobType = c }).ToList()
    };

    public static Worker ActiveWorker(Guid? publicId = null, long id = 101, ICollection<JobType>? capabilities = null) => new()
    {
        Id = id,
        WorkerPublicId = publicId ?? Guid.NewGuid(),
        WorkerName = "worker-a",
        Status = WorkerStatusEnum.Active,
        WorkerCapabilities = capabilities == null ? [] : capabilities.Select(c => new WorkerCapability { WorkerId = id, JobTypeId = c.Id, JobType = c }).ToList()
    };

    public static Job QueuedJob(JobType? jobType = null, long id = 501) => new()
    {
        Id = id,
        JobPublicId = Guid.NewGuid(),
        JobType = jobType ?? EmailJobType(),
        JobTypeId = jobType?.Id ?? 11,
        Payload = "{\"id\":1}",
        Status = JobStatusEnum.Queued
    };
}
