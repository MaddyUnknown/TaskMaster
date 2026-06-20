namespace TaskMaster.Library.Common.Models.JobType
{
    public class JobTypeRef
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }

        public static JobTypeRef Empty => new JobTypeRef();
    }
}
