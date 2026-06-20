using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Models.Workers;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Constants;
using TaskMaster.Library.Consumer.Interfaces;
using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Library.Consumer.Workers
{
    internal class TaskWorker : IWorker
    {
        private bool _disposed = false;

        private IApiHttpClient _httpClient;
        private TaskMasterConsumerOptions _options;
        private IServiceProvider _serviceProvider;

        private string _workerName;
        private WorkerConfiguration _configuration;
        private WorkerDetails? _workerDetails = null;
        private int _heartBeatIntervalSeconds;

        private CancellationTokenSource? _heartBeatCancelToken;

        public TaskWorker(
            string workerName,
            WorkerConfiguration configuration,
            IApiHttpClient httpClient,
            IServiceProvider serviceProvider,
            IOptions<TaskMasterConsumerOptions> options)
        {
            _workerName = workerName;
            _configuration = configuration;
            _httpClient = httpClient;
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var jobResult = await ConsumeAsync(cancellationToken);
                if (jobResult == null) continue;

                if (!_configuration.HandlerMap.TryGetValue((jobResult.JobType.Name, jobResult.JobType.Version), out var entry))
                {
                    throw new InvalidOperationException(ErrorMessage.HandlerNotRegistered(jobResult.JobType.Name, jobResult.JobType.Version));
                }

                using var scope = _serviceProvider.CreateScope();
                var handler = (IJobHandler) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, entry.HandlerType);

                try
                {
                    var payload = JsonConvert.DeserializeObject(jobResult.Payload ?? string.Empty, entry.PayloadType);
                    if (payload == null) throw new Exception(ErrorMessage.JsonParsingError(jobResult.JobId, jobResult.JobType.Name, jobResult.JobType.Version));

                    await handler.HandleAsync(payload, cancellationToken);

                    await CompleteAsync(jobResult);
                }
                catch
                {
                    //TO-DO: Introduce logging
                    await FailAsync(jobResult);
                }
            }
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
                JobTypeCapabilities = _configuration.GetCapabilities()
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


        private async Task<JobConsumeResult?> ConsumeAsync(CancellationToken cancellationToken = default)
        {
            if (_workerDetails == null) await SetupWorker(_workerName);

            var deadline = _options.ConsumerWaitTimeoutMs.HasValue
                ? DateTime.Now.AddMilliseconds(_options.ConsumerWaitTimeoutMs.Value)
                : (DateTime?) null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var job = await _httpClient.PullJob(_workerDetails!.WorkerId);
                if (job != null)
                {
                    return new JobConsumeResult
                    {
                        Payload = job.Payload,
                        JobId = job.JobId,
                        JobType = job.JobType,
                        Status = job.Status
                    };
                }

                if (deadline.HasValue && DateTime.Now >= deadline.Value) return null;

                await Task.Delay(_options.PollingWaitIntervalMs, cancellationToken);
            }

            return null;
        }

        private async Task CompleteAsync(JobConsumeResult jobResult)
        {
            await _httpClient.CompleteJob(jobResult.JobId, _workerDetails!.WorkerId);
        }

        private async Task FailAsync(JobConsumeResult jobResult)
        {
            await _httpClient.FailJob(jobResult.JobId, _workerDetails!.WorkerId);
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
