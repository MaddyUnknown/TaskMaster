namespace TaskMaster.Models.Workers
{
    public class RegisterWorkerRequest
    {
        public string WorkerName { get; set; } = string.Empty;
        public string[] JobTypeCapabilities { get; set; } = Array.Empty<string>();
    }
}
