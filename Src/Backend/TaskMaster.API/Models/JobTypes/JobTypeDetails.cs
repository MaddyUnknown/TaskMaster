namespace TaskMaster.API.Models.JobTypes
{
    public class JobTypeDetails
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }
        public string Schema { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifyDateTime { get; set; }

        public static JobTypeDetails Empty => new JobTypeDetails();
    }
}
