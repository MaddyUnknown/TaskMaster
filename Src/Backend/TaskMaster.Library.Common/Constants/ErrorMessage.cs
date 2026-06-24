using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Constants
{
    internal static class ErrorMessage
    {
        internal static string CacheKeyNotFound(string key) => $"Could not find data for key: '{key}'";
        internal static string JobTypeNotFound(string name, long version) => $"Job Type not found for name: '{name}' and version: '{version}'";
        internal static string ApiBaseUrlRequired() => "TaskMaster API base URL is required.";
        internal static string NetworkError(string endpoint, string message) => $"Network error calling '{endpoint}': {message}";
        internal static string ApiEmptyResponse() => "API returned an empty response.";
        internal static string ApiRequestFailed(string errors) => errors;
        internal static string ApiUnexpectedStatusCode(int statusCode) => $"API returned status code {statusCode}.";
    }
}
