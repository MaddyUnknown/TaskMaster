using TaskMaster.API.Enums;

namespace TaskMaster.API.Models.Jobs
{
    public class UpdateJobStatus
    {
        public Guid JobId { get; set; }
        public JobStatusEnum Status { get; set; }
    }
}
