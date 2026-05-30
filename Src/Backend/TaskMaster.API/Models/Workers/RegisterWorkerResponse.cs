using TaskMaster.API.Enums;

namespace TaskMaster.Models.Workers
{
    public class RegisterWorkerResponse
    {
        public Guid WorkerId { get; set; }
        public WorkerStatusEnum Status { get; set; }
        public int HeartBeatIntervalSeconds { get; set; } = 10;
    }
}
