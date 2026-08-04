using TaskMaster.API.Enums;

namespace TaskMaster.API.Models.Jobs
{
    public class UpdateJobStatusErrorResponse
    {
        public Guid JobId { get; set; }
        public string ErrorReason { get; set; } = string.Empty;
    }
}
