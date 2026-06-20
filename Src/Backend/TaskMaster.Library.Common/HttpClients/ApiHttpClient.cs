using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Configs;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.Interfaces.HttpClients;
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

            var response = await _httpClient.GetAsync(ApiEndpoint.GetJobType(request.JobTypeName, request.JobTypeVersion));
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JobTypeDetails>();
        }

        public async Task<JobDetails> CreateJob(CreateJobRequest request)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint.CreateJob, request);
            response.EnsureSuccessStatusCode();

            var job = await response.Content.ReadFromJsonAsync<JobDetails>();
            if (job == null) throw new InvalidOperationException("Job API returned an empty response.");

            return job;
        }

        public async Task<RegisterWorkerResponse> RegisterWorker(RegisterWorker worker)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint.RegisterWorker, worker);
            response.EnsureSuccessStatusCode();

            var workerRegistrationResponse = await response.Content.ReadFromJsonAsync<RegisterWorkerResponse>();
            if (workerRegistrationResponse == null) throw new InvalidOperationException("Worker API returned an empty response.");

            return workerRegistrationResponse;
        }

        public async Task<WorkerDetails> RemoveWorker(Guid workerId)
        {
            EnsureBaseAddress();

            var response = await _httpClient.DeleteAsync(ApiEndpoint.RemoveWorker(workerId));
            response.EnsureSuccessStatusCode();

            var worker = await response.Content.ReadFromJsonAsync<WorkerDetails>();
            if (worker == null) throw new InvalidOperationException("Worker API returned an empty response.");

            return worker;
        }

        public async Task<JobDetails?> PullJob(Guid workerId)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsync(ApiEndpoint.PullJob(workerId), null);
            if (response.StatusCode == HttpStatusCode.NoContent) return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JobDetails>();
        }

        public async Task<JobDetails> CompleteJob(Guid jobId, Guid workerId)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint.CompleteJob(jobId), new { WorkerId = workerId });
            response.EnsureSuccessStatusCode();

            var job = await response.Content.ReadFromJsonAsync<JobDetails>();
            if (job == null) throw new InvalidOperationException("Job API returned an empty response.");

            return job;
        }

        public async Task<JobDetails> FailJob(Guid jobId, Guid workerId)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint.FailJob(jobId), new { WorkerId = workerId });
            response.EnsureSuccessStatusCode();

            var job = await response.Content.ReadFromJsonAsync<JobDetails>();
            if (job == null) throw new InvalidOperationException("Job API returned an empty response.");

            return job;
        }

        public async Task<HeartbeatActionStatus> WorkerHeartBeat(Guid workerId)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint.WorkerHeartBeat(workerId), string.Empty);
            response.EnsureSuccessStatusCode();

            var heartbeatAction = await response.Content.ReadFromJsonAsync<HeartbeatActionStatus>();
            if (heartbeatAction == null) throw new InvalidOperationException("Worker API returned an empty response.");

            return heartbeatAction;
        }


        private void EnsureBaseAddress()
        {
            if (_httpClient.BaseAddress != null) return;

            if (string.IsNullOrWhiteSpace(_apiConfigOption.Value.ApiBaseUrl))
            {
                throw new InvalidOperationException("TaskMaster API base URL is required.");
            }

            _httpClient.BaseAddress = new Uri(_apiConfigOption.Value.ApiBaseUrl);
        }
    }
}
