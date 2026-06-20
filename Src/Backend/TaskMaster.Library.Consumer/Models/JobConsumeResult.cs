using Newtonsoft.Json;
using TaskMaster.Library.Common.Models.JobType;

namespace TaskMaster.Library.Consumer.Models
{
    public class JobConsumeResult
    {
        public string? Payload { get; set; }
        public Guid JobId { get; set; }
        public JobTypeRef JobType { get; set; } = JobTypeRef.Empty;
        public string Status { get; set; } = string.Empty;

        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Payload ?? string.Empty)!;
        }
    }
}
