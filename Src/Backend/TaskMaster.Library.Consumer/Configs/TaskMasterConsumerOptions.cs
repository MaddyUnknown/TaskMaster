namespace TaskMaster.Library.Consumer.Configs
{
    public class TaskMasterConsumerOptions
    {
        public string ApiBaseUrl { get; set; } = string.Empty;

        public int PollingWaitIntervalMs { get; set; } = 1000;

        public int? ConsumerWaitTimeoutMs { get; set; }
    }
}
