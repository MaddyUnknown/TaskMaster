using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Models.Common;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Common.Models.Workers;
using TaskMaster.Library.Common.Models.Auth;

namespace TaskMaster.Library.Common.HttpClients
{
    internal class ApiHttpClient : IApiHttpClient
    {
        private IOptions<AppConfig> _appConfigOption;
        private IHttpClientFactory _httpClientFactory;

        public ApiHttpClient(IOptions<AppConfig> appConfigOption, IHttpClientFactory httpClientFactory)
        {
            _appConfigOption = appConfigOption;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AuthConfigDetails?> GetAuthConfig()
        {
            var httpClient = CreateHttpClient("public");
            var endpoint = ApiEndpoint.GetAuthConfig();
            var response = await SendAsync(endpoint, () => httpClient.GetAsync(endpoint));
            var wrapper = await EnsureSuccessAsync<AuthConfigDetails>(response);
            return wrapper.Data;
        }

        public async Task<JobTypeDetails?> GetJobType(GetJobTypeRequest request)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.GetJobType(request.JobTypeName, request.JobTypeVersion);
            var response = await SendAsync(endpoint, () => httpClient.GetAsync(endpoint));
            var wrapper = await EnsureSuccessAsync<JobTypeDetails[]>(response);
            return wrapper.Data?.FirstOrDefault();
        }

        public async Task<JobDetails> CreateJob(CreateJobRequest request)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.CreateJob;
            var response = await SendAsync(endpoint, () => httpClient.PostAsJsonAsync(endpoint, request));
            var wrapper = await EnsureSuccessAsync<JobDetails>(response);
            return wrapper.Data!;
        }

        public async Task<RegisterWorkerResponse> RegisterWorker(RegisterWorker worker)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.RegisterWorker;
            var response = await SendAsync(endpoint, () => httpClient.PostAsJsonAsync(endpoint, worker));
            var wrapper = await EnsureSuccessAsync<RegisterWorkerResponse>(response);
            return wrapper.Data!;
        }

        public async Task<WorkerDetails> RemoveWorker(Guid workerId)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.RemoveWorker(workerId);
            var response = await SendAsync(endpoint, () => httpClient.DeleteAsync(endpoint));
            var wrapper = await EnsureSuccessAsync<WorkerDetails>(response);
            return wrapper.Data!;
        }

        public async Task<JobDetails?> PullJob(Guid workerId)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.PullJob(workerId);
            var response = await SendAsync(endpoint, () => httpClient.PostAsync(endpoint, null));
            if (response.StatusCode == HttpStatusCode.NoContent) return null;
            var wrapper = await EnsureSuccessAsync<JobDetails?>(response);
            return wrapper.Data;
        }

        public async Task<IEnumerable<JobDetails>> PullJobs(Guid workerId, int maxJobs)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.PullJobs(workerId, maxJobs);
            var response = await SendAsync(endpoint, () => httpClient.PostAsync(endpoint, null));
            var wrapper = await EnsureSuccessAsync<IEnumerable<JobDetails>>(response);
            return wrapper.Data!;
        }

        public async Task<JobDetails> CompleteJob(Guid jobId, Guid workerId)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.CompleteJob(jobId);
            var response = await SendAsync(endpoint, () => httpClient.PostAsJsonAsync(endpoint, new { WorkerId = workerId }));
            var wrapper = await EnsureSuccessAsync<JobDetails>(response);
            return wrapper.Data!;
        }

        public async Task<JobDetails> FailJob(Guid jobId, Guid workerId)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.FailJob(jobId);
            var response = await SendAsync(endpoint, () => httpClient.PostAsJsonAsync(endpoint, new { WorkerId = workerId }));
            var wrapper = await EnsureSuccessAsync<JobDetails>(response);
            return wrapper.Data!;
        }

        public async Task<BulkUpdateJobStatusResponse> BulkUpdateJobStatus(BulkUpdateJobStatusRequest request)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.BatchUpdateJobStatus;
            var response = await SendAsync(endpoint, () => httpClient.PostAsJsonAsync(endpoint, request));
            var wrapper = await EnsureSuccessAsync<BulkUpdateJobStatusResponse>(response);
            return wrapper.Data!;
        }

        public async Task<HeartbeatActionStatus> WorkerHeartBeat(Guid workerId)
        {
            var httpClient = CreateHttpClient();
            var endpoint = ApiEndpoint.WorkerHeartBeat(workerId);
            var response = await SendAsync(endpoint, () => httpClient.PostAsJsonAsync(endpoint, string.Empty));
            var wrapper = await EnsureSuccessAsync<HeartbeatActionStatus>(response);
            return wrapper.Data!;
        }

        private HttpClient CreateHttpClient(string name="protected")
        {
            var httpClient = _httpClientFactory.CreateClient(name);
            if (httpClient == null) throw new InvalidOperationException(ErrorMessage.HttpClientCreateFailed());

            if (string.IsNullOrWhiteSpace(_appConfigOption.Value.ApiBaseUrl))
                throw new InvalidOperationException(ErrorMessage.ApiBaseUrlRequired());

            httpClient.BaseAddress = new Uri(_appConfigOption.Value.ApiBaseUrl);

            return httpClient;
        }

        private static async Task<HttpResponseMessage> SendAsync(
            string endpoint, Func<Task<HttpResponseMessage>> send)
        {
            try
            {
                return await send();
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null)
            {
                throw new InvalidOperationException(
                    ErrorMessage.NetworkError(endpoint, ex.Message), ex);
            }
        }

        private static async Task<ApiResponse<T>> EnsureSuccessAsync<T>(
            HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await TryReadErrorMessageAsync(response);
                throw new InvalidOperationException(errorMessage);
            }

            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
            if (wrapper == null) throw new InvalidOperationException(ErrorMessage.ApiEmptyResponse());

            if (!wrapper.IsSuccess) throw new InvalidOperationException(ErrorMessage.ApiRequestFailed(string.Join("; ", wrapper.ErrorMessages)));

            return wrapper;
        }

        private static async Task<string> TryReadErrorMessageAsync(
            HttpResponseMessage response)
        {
            try
            {
                var errorWrapper = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

                if (errorWrapper?.ErrorMessages.Count > 0) return string.Join("; ", errorWrapper.ErrorMessages);
            }
            catch (JsonException)
            {

            }

            return ErrorMessage.ApiUnexpectedStatusCode((int)response.StatusCode);
        }
    }
}
