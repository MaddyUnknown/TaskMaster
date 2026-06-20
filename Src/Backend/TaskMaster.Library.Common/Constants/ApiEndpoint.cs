using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Constants
{
    internal static class ApiEndpoint
    {
        // Job Type
        internal static string GetJobType(string name, long version) => $"api/job-types?name={WebUtility.UrlEncode(name)}&version={WebUtility.UrlEncode(version.ToString())}";
        
        // Job
        internal const string CreateJob = "api/jobs";
        internal static string PullJob(Guid workerId) => $"api/jobs/pull?workerId={workerId}";
        internal static string CompleteJob(Guid jobId) => $"api/jobs/{jobId}/complete";
        internal static string FailJob(Guid jobId) => $"api/jobs/{jobId}/fail";

        // Worker
        internal const string RegisterWorker = "api/workers/register";
        internal static string RemoveWorker(Guid workerId) => $"api/workers/{workerId}";
        internal static string WorkerHeartBeat(Guid workerId) => $"api/workers/{workerId}/heartbeat";
    }
}
