namespace TaskMaster.API.Models.Jobs
{
    public class JobStatusCounts
    {
        public int Queued { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public int Total => Queued + InProgress + Completed + Failed;
    }
}
