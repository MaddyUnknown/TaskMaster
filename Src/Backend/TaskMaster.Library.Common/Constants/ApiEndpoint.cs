using System.Net;

namespace TaskMaster.Library.Common.Constants
{
    internal static class ApiEndpoint
    {
        // Job Type
        internal static string GetJobType(string name, long version) => $"/api/job-types?name={WebUtility.UrlEncode(name)}&version={WebUtility.UrlEncode(version.ToString())}";
        
        // Job
        internal const string CreateJob = "/api/jobs";
        internal static string PullJob(Guid workerId) => $"/api/jobs/pull?workerId={workerId}";
        internal static string PullJobs(Guid workerId, int maxJobs) => $"/api/jobs/pull?workerId={workerId}&maxJobs={maxJobs}";
        internal static string CompleteJob(Guid jobId) => $"/api/jobs/{jobId}/complete";
        internal static string FailJob(Guid jobId) => $"/api/jobs/{jobId}/fail";
        internal const string BatchUpdateJobStatus = "/api/jobs/status/bulk";

        // Worker
        internal const string RegisterWorker = "/api/workers/register";
        internal static string RemoveWorker(Guid workerId) => $"/api/workers/{workerId}";
        internal static string WorkerHeartBeat(Guid workerId) => $"/api/workers/{workerId}/heartbeat";
    }
}
