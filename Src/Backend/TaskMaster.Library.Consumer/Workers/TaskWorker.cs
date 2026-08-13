using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading.Channels;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Common.Models.Workers;
using TaskMaster.Library.Consumer.Configs;
using TaskMaster.Library.Consumer.Constants;
using TaskMaster.Library.Consumer.Interfaces;
using TaskMaster.Library.Consumer.Models;

namespace TaskMaster.Library.Consumer.Workers
{
    internal class TaskWorker : IWorker
    {
        private const int MAX_RETRY_BACKOFF_TIME_MS = 30_000;
        private const int MAX_HEARTBEAT_COUNT = 2;

        private IApiHttpClient _httpClient;
        private TaskMasterConsumerOptions _options;
        private IServiceProvider _serviceProvider;

        private string _workerName;
        private WorkerConfiguration _configuration;

        private WorkerDetails? _workerDetails = null;
        private int _heartBeatIntervalSeconds;

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
            if (_workerDetails != null) throw new InvalidOperationException(ErrorMessage.WorkerAlreadyRunning());

            // Setup worker in server
            await SetupWorker(_workerName);
            var heartBeatCancelledToken =  SetupHeartBeat(cancellationToken);
            var processCancellationTokenSource  = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, heartBeatCancelledToken);

            var channelCapacity = _options.MaxConcurrentHandlers * _options.PrefetchJobPerHandler;
            var totalJobHandler = _options.MaxConcurrentHandlers;

            // Channels
            var jobChannel = Channel.CreateBounded<JobDetails>(new BoundedChannelOptions(channelCapacity) { FullMode = BoundedChannelFullMode.Wait });
            var statusChannel = Channel.CreateUnbounded<JobDetails>();

            // Readers and writer setup.
            var jobReaderTask = StartJobReaderTask(jobChannel.Writer, () => channelCapacity - jobChannel.Reader.Count, processCancellationTokenSource.Token);
            var jobHandlerTask = StartJobHandlerTask(jobChannel.Reader, totalJobHandler, statusChannel.Writer);
            var statusSyncTask = StartStatusSyncTask(statusChannel.Reader, channelCapacity, processCancellationTokenSource.Token);

            // Wait for pending queue processing completion
            await Task.WhenAll(jobReaderTask, jobHandlerTask, statusSyncTask);

            // Clear worker resource in server
            await TearDownWorker();
        }


        #region Worker Setup and Teardown

        private async Task SetupWorker(string workerName)
        {
            var response = await _httpClient.RegisterWorker(new RegisterWorker
            {
                WorkerName = _workerName,
                JobTypeCapabilities = _configuration.GetCapabilities()
            });

            _workerDetails = response.WorkerDetails;
            _heartBeatIntervalSeconds = response.HeartBeatIntervalSeconds;
        }

        private async Task TearDownWorker()
        {
            if (_workerDetails == null) return;

            await _httpClient.RemoveWorker(_workerDetails.WorkerId);
            _workerDetails = null;
        }

        private CancellationToken SetupHeartBeat(CancellationToken ct)
        {
            if (_workerDetails == null) throw new InvalidOperationException(ErrorMessage.WorkerNotInitiated());

            var heartBeatCancellationTokenSource = new CancellationTokenSource();
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_heartBeatIntervalSeconds));

            Task.Run(async () =>
            {
                try
                {
                    int attempts = 0;

                    while (await timer.WaitForNextTickAsync(ct))
                    {
                        HeartbeatActionStatus? result = null;
                        
                        try
                        {
                            result = await _httpClient.WorkerHeartBeat(_workerDetails.WorkerId);
                        }
                        catch
                        {

                        }

                        attempts = (result?.ActionStatus != EnumConstants.ActionStatusEnum.Ok) ? attempts + 1 : 0;

                        if(attempts >= MAX_HEARTBEAT_COUNT)
                        {
                            heartBeatCancellationTokenSource.Cancel();
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {

                }
            });

            return heartBeatCancellationTokenSource.Token;
        }

        #endregion


        #region Processing Pipeline Tasks
        
        private async Task StartJobReaderTask(ChannelWriter<JobDetails> output, Func<int> availableCapacityFn, CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.PollingWaitIntervalMs));

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        var availableCapacity = availableCapacityFn();
                        if (availableCapacity <= 0) continue;

                        var jobs = await _httpClient.PullJobs(_workerDetails!.WorkerId, availableCapacity);

                        if (jobs != null && jobs.Count() > 0)
                        {
                            foreach (var job in jobs)
                            {
                                // No cancel token added as need to clear up rest of the pulled jobs
                                await output.WriteAsync(job);
                            }
                        }
                    }
                    catch
                    {
                        // Log to be added to track such failures
                    }
                }
            }
            catch(OperationCanceledException)
            {
                // Received when cancellation token is activated.
            }

            output.TryComplete();
        }
        
        private async Task StartJobHandlerTask(ChannelReader<JobDetails> input, int maxConcurrentJobHandlers, ChannelWriter<JobDetails> output)
        {
            var handlers = Enumerable.Range(0, maxConcurrentJobHandlers).Select(_ => RunHandlerAsync(input, output)).ToArray();
            await Task.WhenAll(handlers);
            output.TryComplete();
        }
        
        private async Task StartStatusSyncTask(ChannelReader<JobDetails> input, int maxJobSyncBatchSize, CancellationToken ct)
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.ResultFlushIntervalMs));
            var syncList = new List<JobDetails>();

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        while (syncList.Count() < maxJobSyncBatchSize && input.TryRead(out JobDetails? job))
                        {
                            if (job == null) break;
                            syncList.Add(job);
                        }

                        await FlushBufferAsync(syncList);
                        syncList.Clear();
                    }
                    catch
                    {
                        // Log to be added to track such failures
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Received when cancellation token is activated.
            }

            // Clear all data that is queued
            while (await input.WaitToReadAsync())
            {
                while (input.TryRead(out JobDetails? job))
                {
                    if (job == null) break;
                    syncList.Add(job);
                }
            }

            await FlushBufferAsync(syncList);
            syncList.Clear();
        }

        #endregion


        #region Processing Pipeline Helper methods
        
        private async Task RunHandlerAsync(ChannelReader<JobDetails> input, ChannelWriter<JobDetails> output)
        {
            while (await input.WaitToReadAsync())
            {
                while (input.TryRead(out JobDetails? job))
                {
                    if (job == null) continue;

                    if (!_configuration.HandlerMap.TryGetValue((job.JobType.Name, job.JobType.Version), out var handlerEntity))
                    {
                        job.Status = EnumConstants.JobStatusEnum.Failed;
                        await output.WriteAsync(job);
                        continue;
                    }

                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var handler = (IJobHandler)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerEntity.HandlerType);
                            var payload = JsonConvert.DeserializeObject(job.Payload ?? string.Empty, handlerEntity.PayloadType);

                            if (payload == null)
                                throw new InvalidOperationException(ErrorMessage.JsonParsingError(job.JobId, job.JobType.Name, job.JobType.Version));

                            await handler.HandleAsync(payload);
                        }

                        job.Status = EnumConstants.JobStatusEnum.Completed;
                    }
                    catch
                    {
                        job.Status = EnumConstants.JobStatusEnum.Failed;
                    }

                    await output.WriteAsync(job);
                }
            }
        }

        private async Task FlushBufferAsync(IEnumerable<JobDetails> jobDetails)
        {
            if (jobDetails.Count() == 0) return;
            var batch = jobDetails.Select(j => new UpdateJobStatus { JobId = j.JobId, Status = j.Status }).ToList();

            var request = new BulkUpdateJobStatusRequest
            {
                WorkerId = _workerDetails!.WorkerId,
                JobStatuses = batch
            };

            var retryCount = 0;
            var maxRetryCount = Math.Max(1, _options.MaxResultRetries+1);
            var backoffMs = _options.ReporterBackoffBaseMs;

            while (retryCount < maxRetryCount)
            {
                try
                {
                    await _httpClient.BulkUpdateJobStatus(request);
                    break;
                }
                catch
                {
                    retryCount++;
                    if (retryCount > maxRetryCount) return;

                    await Task.Delay(backoffMs);
                    backoffMs = Math.Min(backoffMs * 2, MAX_RETRY_BACKOFF_TIME_MS);
                }
            }
        }
        
        #endregion
    }
}
