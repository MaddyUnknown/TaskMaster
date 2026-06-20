using TaskMaster.Library.Common.Models.JobType;

namespace TaskMaster.Library.Consumer.Models
{
    public class JobConsumeResult<T>
    {
        public T Data { get; set; } = default!;
        public Guid JobId { get; set; }
        public JobTypeRef JobType { get; set; } = JobTypeRef.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
