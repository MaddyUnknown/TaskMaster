namespace TaskMaster.API.Models.Dashboard
{
    public class JobStatsResponse
    {
        public string Timezone { get; set; } = string.Empty;

        public DateTime WindowStartUtc { get; set; }

        public DateTime WindowEndUtc { get; set; }

        public int BucketSizeMinutes { get; set; }

        public IReadOnlyList<JobStatsItem> Buckets { get; set; } = Array.Empty<JobStatsItem>();
    }
}