using TaskMaster.Library.Common.Configs;

namespace TaskMaster.Library.Consumer.Configs
{
    public class TaskMasterConsumerOptions
    {
        public string ApiBaseUrl { get; set; } = string.Empty;

        public TaskMasterConsumerAuthOptions Auth { get; set; } = TaskMasterConsumerAuthOptions.Empty;

        public int MaxConcurrentHandlers { get; set; } = 5;
        public int PrefetchJobPerHandler { get; set; } = 1;
        public int MaxResultRetries { get; set; } = 5;

        public int PollingWaitIntervalMs { get; set; } = 500;
        public int ResultFlushIntervalMs { get; set; } = 200;
        public int ReporterBackoffBaseMs { get; set; } = 200;

    }
}
