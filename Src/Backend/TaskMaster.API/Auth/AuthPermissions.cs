namespace TaskMaster.API.Auth;

public static class AuthPermissions
{
    public const string ReadJobs = "job.read";
    public const string ReadJobStats = "job.stats";
    public const string CreateJob = "job.create";
    public const string PullJobs = "job.pull";
    public const string ReportJobs = "job.report";

    public const string ReadWorkers = "worker.read";
    public const string ReadWorkerStats = "worker.stats";
    public const string RegisterWorkers = "worker.register";
    public const string WorkersHeartbeat = "worker.heartbeat";
    public const string RemoveWorker = "worker.remove";

    public const string ReadJobTypes = "jobtype.read";
    public const string CreateJobTypes = "jobtype.create";

    public const string ReadDashboard = "dashboard.read";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ReadJobs,
        ReadJobStats,
        CreateJob,
        PullJobs,
        ReportJobs,
        ReadWorkers,
        ReadWorkerStats,
        RegisterWorkers,
        WorkersHeartbeat,
        RemoveWorker,
        ReadJobTypes,
        CreateJobTypes,
        ReadDashboard
    };
}