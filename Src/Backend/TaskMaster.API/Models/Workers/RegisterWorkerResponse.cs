namespace TaskMaster.API.Models.Workers
{
    public class RegisterWorkerResponse
    {
        public WorkerDetails WorkerDetails { get; set; } = WorkerDetails.Empty;
        public int HeartBeatIntervalSeconds { get; set; }
    }
}
