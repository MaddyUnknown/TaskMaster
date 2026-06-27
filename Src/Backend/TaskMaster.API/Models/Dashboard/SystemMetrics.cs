namespace TaskMaster.API.Models.Dashboard
{
    public class SystemMetrics
    {
        public double SuccessRate { get; set; }
        public double AvgProcessingTime { get; set; }
        public int ActiveWorkers { get; set; }
        public int QueueDepth { get; set; }
    }
}
