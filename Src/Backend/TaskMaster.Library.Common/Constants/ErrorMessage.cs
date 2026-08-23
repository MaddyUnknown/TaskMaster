using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Common.Constants
{
    internal static class ErrorMessage
    {
        internal static string JobTypeNotFound(string name, long version) => $"Job Type not found for name: '{name}' and version: '{version}'";
        internal static string HttpClientCreateFailed() => "TaskMaster API httpClient creation failed.";
        internal static string ApiBaseUrlRequired() => "TaskMaster API base URL is required.";
        internal static string NetworkError(string endpoint, string message) => $"Network error calling '{endpoint}': {message}";
        internal static string ApiEmptyResponse() => "API returned an empty response.";
        internal static string ApiRequestFailed(string errors) => errors;
        internal static string ApiUnexpectedStatusCode(int statusCode) => $"API returned status code {statusCode}.";
        internal static string InvalidAuthMode(string authMode) => $"Invalid auth mode '{authMode}'";

        internal static string AuthTokenEndpointNotFound(string authority) => $"Could not discover the token endpoint for authority '{authority}'.";
        internal static string AuthTokenRequestFailed(int statusCode) => $"Token request failed with status code {statusCode}.";
        internal static string AuthApiConfigUnavailable() => "Could not retrieve authentication configuration from the TaskMaster API.";
        internal static string AuthConfigFetchError() => "Error fetching auth config";
        internal static string AuthModeNotConfigured(string mode) => $"Configuration for auth mode '{mode}' missing in config";
        internal static string AuthParameterNotConfigured(string parameter, string mode) => $"'{parameter}' for auth mode '{mode}' missing";

    }
}
