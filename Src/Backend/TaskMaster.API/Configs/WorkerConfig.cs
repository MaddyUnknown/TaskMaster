namespace TaskMaster.API.Configs
{
    public class WorkerConfig
    {
        public int HeartBeatIntervalSeconds { get; set; }
        public int WorkerExpiryIntervalSeconds { get; set; }
        public int WorkerExpiryCheckIntervalSeconds { get; set; }
    }
}
