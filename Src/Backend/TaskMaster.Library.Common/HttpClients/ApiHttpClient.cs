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

namespace TaskMaster.Library.Common.HttpClients
{
    internal class ApiHttpClient : IApiHttpClient
    {
        private IOptions<ApiConfig> _apiConfigOption;
        private HttpClient _httpClient;

        internal ApiHttpClient(IOptions<ApiConfig> apiConfigOption)
            : this(apiConfigOption, new HttpClient())
        {
        }

        internal ApiHttpClient(IOptions<ApiConfig> apiConfigOption, HttpClient httpClient)
        {
            _apiConfigOption = apiConfigOption;
            _httpClient = httpClient;
        }

        public async Task<GetJobTypeResponse?> GetJobType(GetJobTypeRequest request)
        {
            EnsureBaseAddress();

            var response = await _httpClient.GetAsync(ApiEndpoint.GetJobType(request.JobTypeName, request.JobTypeVersion));
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GetJobTypeResponse>();
        }

        public async Task<CreateJobResponse> CreateJob(CreateJobRequest request)
        {
            EnsureBaseAddress();

            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint.CreateJob, request);
            response.EnsureSuccessStatusCode();

            var job = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
            if (job == null) throw new InvalidOperationException("Job API returned an empty response.");

            return job;
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
