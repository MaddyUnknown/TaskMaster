namespace TaskMaster.API.Models.JobTypes
{
    public class CreateJobType
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }
        public string Schema { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
    }
}
