using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Models.Workers;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Interfaces;
using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Library.Consumer.Consumers
{
    public class TaskWorker<T> : IWorker<T>, IAsyncDisposable where T : new()
    {
        private bool _disposed = false;

        private IJobTypeSchemaRegistry _schemaRegistry;
        private IApiHttpClient _httpClient;
        private TaskMasterConsumerOptions _options;

        private string _workerName;
        private WorkerDetails? _workerDetails = null;
        private int _heartBeatIntervalSeconds;

        private CancellationTokenSource? _heartBeatCancelToken;

        public TaskWorker(string workerName)
        {
            var serviceProvider = TaskMasterConsumer.ServiceProducer;
            _schemaRegistry = serviceProvider.GetRequiredService<IJobTypeSchemaRegistry>();
            _httpClient = serviceProvider.GetRequiredService<IApiHttpClient>();
            _options = serviceProvider.GetRequiredService<IOptions<TaskMasterConsumerOptions>>().Value;

            _workerName = workerName;
        }

        public TaskWorker(string workerName, IJobTypeSchemaRegistry schemaRegistry, IApiHttpClient httpClient, IOptions<TaskMasterConsumerOptions> options)
        {
            _schemaRegistry = schemaRegistry;
            _httpClient = httpClient;
            _options = options.Value;

            _workerName = workerName;
        }

        public async Task<JobConsumeResult<T>?> ConsumeAsync()
        {
            if (_workerDetails == null) await SetupWorker(_workerName);

            var deadline = _options.ConsumerWaitTimeoutMs.HasValue
                ? DateTime.UtcNow.AddMilliseconds(_options.ConsumerWaitTimeoutMs.Value)
                : (DateTime?)null;

            while (true)
            {
                var job = await _httpClient.PullJob(_workerDetails!.WorkerId);
                if (job != null)
                {
                    return new JobConsumeResult<T>
                    {
                        Data = JsonConvert.DeserializeObject<T>(job.Payload ?? string.Empty)!,
                        JobId = job.JobId,
                        JobType = job.JobType,
                        Status = job.Status
                    };
                }

                if (deadline.HasValue && DateTime.UtcNow >= deadline.Value)
                    return null;

                await Task.Delay(_options.PollingWaitIntervalMs);
            }
        }

        public async Task CompleteAsync(JobConsumeResult<T> jobResult)
        {
            await _httpClient.CompleteJob(jobResult.JobId, _workerDetails!.WorkerId);
        }

        public async Task FailAsync(JobConsumeResult<T> jobResult)
        {
            await _httpClient.FailJob(jobResult.JobId, _workerDetails!.WorkerId);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await TearDownWorker();
            _disposed = true;
        }

        private async Task SetupWorker(string workerName)
        {
            var response = await _httpClient.RegisterWorker(new RegisterWorker
            {
                WorkerName = _workerName,
                JobTypeCapabilities = Enumerable.Empty<JobTypeRef>()
            });

            _workerDetails = response.WorkerDetails;
            _heartBeatIntervalSeconds = response.HeartBeatIntervalSeconds;

            SetupHeartBeat();
        }

        private async Task TearDownWorker()
        {
            if (_workerDetails == null) return;

            _heartBeatCancelToken?.Cancel();

            await _httpClient.RemoveWorker(_workerDetails.WorkerId);
        }

        private void SetupHeartBeat()
        {
            if (_workerDetails == null) return;

            _heartBeatCancelToken = new CancellationTokenSource();
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_heartBeatIntervalSeconds));

            Task.Run(async () =>
            {
                while (await timer.WaitForNextTickAsync(_heartBeatCancelToken.Token))
                {
                    var result = await _httpClient.WorkerHeartBeat(_workerDetails.WorkerId);
                    if (result.ActionStatus != "Ok") throw new Exception();
                }
            });
        }
    }
}
