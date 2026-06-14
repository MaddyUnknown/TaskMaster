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
        internal static string GetJobType(string name, long version) => $"api/job-types?name={WebUtility.UrlEncode(name)}&version={WebUtility.UrlEncode(version.ToString())}";
        internal const string CreateJob = "api/jobs";
    }
}
