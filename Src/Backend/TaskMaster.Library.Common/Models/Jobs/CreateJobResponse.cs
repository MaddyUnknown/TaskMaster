namespace TaskMaster.Library.Common.Models.Jobs
{
    public class CreateJobResponse
    {
        public Guid JobId { get; set; }
        public JobTypeRef JobType { get; set; } = new();
        public string? Payload { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
