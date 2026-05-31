namespace TaskMaster.API.Models.JobTypes
{
    public class GetJobType
    {
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }

        public static GetJobType Empty => new GetJobType();
    }
}
