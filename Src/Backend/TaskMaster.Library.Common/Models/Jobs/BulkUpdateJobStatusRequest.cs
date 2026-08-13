namespace TaskMaster.Library.Common.Models.Jobs
{
    public class BulkUpdateJobStatusRequest
    {
        public Guid WorkerId { get; set; }
        public List<UpdateJobStatus> JobStatuses { get; set; } = new();
    }
}
