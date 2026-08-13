namespace TaskMaster.API.Models.Workers
{
    public class WorkerStatusCounts
    {
        public int Active { get; set; }
        public int InActive { get; set; }
        public int Total => Active + InActive;
    }
}
