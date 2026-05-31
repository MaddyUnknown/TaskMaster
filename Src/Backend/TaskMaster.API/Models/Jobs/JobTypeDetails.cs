namespace TaskMaster.API.Models.Jobs
{
    public class JobTypeDetails
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }

        public static JobTypeDetails Empty => new JobTypeDetails();
    }
}
