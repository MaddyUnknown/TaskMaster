using TaskMaster.Library.Common.Constants;

namespace TaskMaster.Library.Common.Models.Jobs
{
    public class UpdateJobStatus
    {
        public Guid JobId { get; set; }
        public string Status { get; set; } = EnumConstants.JobStatusEnum.Completed;
    }
}
