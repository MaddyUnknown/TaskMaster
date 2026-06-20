namespace TaskMaster.API.Models.JobTypes
{
    public class JobTypeRef
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }

        public static JobTypeRef Empty => new JobTypeRef();
    }
}
