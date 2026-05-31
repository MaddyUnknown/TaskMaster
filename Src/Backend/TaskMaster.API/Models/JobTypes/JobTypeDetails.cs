namespace TaskMaster.API.Models.JobTypes
{
    public class JobTypeDetails
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }
        public string Schema { get; set; } = string.Empty;

        public static JobTypeDetails Empty => new JobTypeDetails();
    }
}
