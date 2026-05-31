using TaskMaster.API.Models.Jobs;

namespace TaskMaster.API.Models.Workers
{
    public class RegisterWorkerRequest
    {
        public string WorkerName { get; set; } = string.Empty;
        public IEnumerable<JobTypeDetails> JobTypeCapabilities { get; set; } = Enumerable.Empty<JobTypeDetails>();
    }
}
