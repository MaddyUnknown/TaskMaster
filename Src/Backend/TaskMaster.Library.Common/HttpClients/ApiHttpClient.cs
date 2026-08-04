using System.Linq;
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

namespace TaskMaster.Library.Common.HttpClients
{
    internal class ApiHttpClient : IApiHttpClient
    {
        private IOptions<ApiConfig> _apiConfigOption;
        private HttpClient _httpClient;

        public ApiHttpClient(IOptions<ApiConfig> apiConfigOption)
            : this(apiConfigOption, new HttpClient())
        {
        }

        public ApiHttpClient(IOptions<ApiConfig> apiConfigOption, HttpClient httpClient)
        {
            _apiConfigOption = apiConfigOption;
            _httpClient = httpClient;
        }

        public async Task<JobTypeDetails?> GetJobType(GetJobTypeRequest request)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.GetJobType(request.JobTypeName, request.JobTypeVersion);
            var response = await SendAsync(endpoint, () => _httpClient.GetAsync(endpoint));
            var wrapper = await EnsureSuccessAsync<JobTypeDetails[]>(response);
            return wrapper.Data?.FirstOrDefault();
        }

        public async Task<JobDetails> CreateJob(CreateJobRequest request)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.CreateJob;
            var response = await SendAsync(endpoint, () => _httpClient.PostAsJsonAsync(endpoint, request));
            var wrapper = await EnsureSuccessAsync<JobDetails>(response);
            return wrapper.Data!;
        }

        public async Task<RegisterWorkerResponse> RegisterWorker(RegisterWorker worker)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.RegisterWorker;
            var response = await SendAsync(endpoint, () => _httpClient.PostAsJsonAsync(endpoint, worker));
            var wrapper = await EnsureSuccessAsync<RegisterWorkerResponse>(response);
            return wrapper.Data!;
        }

        public async Task<WorkerDetails> RemoveWorker(Guid workerId)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.RemoveWorker(workerId);
            var response = await SendAsync(endpoint, () => _httpClient.DeleteAsync(endpoint));
            var wrapper = await EnsureSuccessAsync<WorkerDetails>(response);
            return wrapper.Data!;
        }

        public async Task<JobDetails?> PullJob(Guid workerId)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.PullJob(workerId);
            var response = await SendAsync(endpoint, () => _httpClient.PostAsync(endpoint, null));
            if (response.StatusCode == HttpStatusCode.NoContent) return null;
            var wrapper = await EnsureSuccessAsync<JobDetails?>(response);
            return wrapper.Data;
        }

        public async Task<IEnumerable<JobDetails>> PullJobs(Guid workerId, int maxJobs)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.PullJobs(workerId, maxJobs);
            var response = await SendAsync(endpoint, () => _httpClient.PostAsync(endpoint, null));
            var wrapper = await EnsureSuccessAsync<IEnumerable<JobDetails>>(response);
            return wrapper.Data!;
        }

        public async Task<JobDetails> CompleteJob(Guid jobId, Guid workerId)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.CompleteJob(jobId);
            var response = await SendAsync(endpoint, () => _httpClient.PostAsJsonAsync(endpoint, new { WorkerId = workerId }));
            var wrapper = await EnsureSuccessAsync<JobDetails>(response);
            return wrapper.Data!;
        }

        public async Task<JobDetails> FailJob(Guid jobId, Guid workerId)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.FailJob(jobId);
            var response = await SendAsync(endpoint, () => _httpClient.PostAsJsonAsync(endpoint, new { WorkerId = workerId }));
            var wrapper = await EnsureSuccessAsync<JobDetails>(response);
            return wrapper.Data!;
        }

        public async Task<BulkUpdateJobStatusResponse> BulkUpdateJobStatus(BulkUpdateJobStatusRequest request)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.BatchUpdateJobStatus;
            var response = await SendAsync(endpoint, () => _httpClient.PostAsJsonAsync(endpoint, request));
            var wrapper = await EnsureSuccessAsync<BulkUpdateJobStatusResponse>(response);
            return wrapper.Data!;
        }

        public async Task<HeartbeatActionStatus> WorkerHeartBeat(Guid workerId)
        {
            EnsureBaseAddress();
            var endpoint = ApiEndpoint.WorkerHeartBeat(workerId);
            var response = await SendAsync(endpoint, () => _httpClient.PostAsJsonAsync(endpoint, string.Empty));
            var wrapper = await EnsureSuccessAsync<HeartbeatActionStatus>(response);
            return wrapper.Data!;
        }

        private void EnsureBaseAddress()
        {
            if (_httpClient.BaseAddress != null) return;

            if (string.IsNullOrWhiteSpace(_apiConfigOption.Value.ApiBaseUrl))
                throw new InvalidOperationException(ErrorMessage.ApiBaseUrlRequired());

            _httpClient.BaseAddress = new Uri(_apiConfigOption.Value.ApiBaseUrl);
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
