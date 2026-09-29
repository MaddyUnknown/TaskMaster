using TaskMaster.API.Enums;

namespace TaskMaster.API.Models.Jobs
{
    /// <summary>
    /// Flat projection returned by the job claim statement. The SQL aliases the job type
    /// columns with a <c>JobType_</c> prefix so a single result set can carry both the job
    /// and its job type without a second round trip.
    /// </summary>
    public class JobPullDto
    {
        public long Id { get; set; }
        public Guid JobPublicId { get; set; }
        public string? Payload { get; set; }
        public JobStatusEnum Status { get; set; }
        public long JobTypeId { get; set; }
        public long? AssignedWorkerId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifyDateTime { get; set; }

        public long JobType_Id { get; set; }
        public string JobType_Name { get; set; } = string.Empty;
        public long JobType_Version { get; set; }
        public string JobType_Schema { get; set; } = string.Empty;
        public string? JobType_Description { get; set; }
        public DateTime JobType_CreatedDateTime { get; set; }
        public DateTime? JobType_ModifyDateTime { get; set; }
    }
}
