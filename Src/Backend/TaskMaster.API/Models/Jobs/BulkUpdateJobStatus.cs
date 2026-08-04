namespace TaskMaster.API.Models.Jobs
{
    public class BulkUpdateJobStatus
    {
        public Guid WorkerId { get; set; }
        public List<UpdateJobStatus> JobStatuses { get; set; } = new();
    }
}
