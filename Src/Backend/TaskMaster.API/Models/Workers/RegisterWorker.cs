using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Models.Workers
{
    public class RegisterWorker
    {
        public string WorkerName { get; set; } = string.Empty;
        public IEnumerable<GetJobType> JobTypeCapabilities { get; set; } = Enumerable.Empty<GetJobType>();
    }
}
