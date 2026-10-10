namespace TaskMaster.API.Models.Dashboard
{
    public class JobStatsItem
    {
        public DateTime BucketStart { get; set; }
        public DateTime BucketEnd { get; set; }
        public string Label { get; set; } = string.Empty;
        public int JobCount { get; set; }
    }
}
