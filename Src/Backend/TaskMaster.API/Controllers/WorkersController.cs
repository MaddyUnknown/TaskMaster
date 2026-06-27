using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Controllers
{
    [ApiController]
    [Route("api/workers")]
    public class WorkersController : ControllerBase
    {
        private IWorkerService _workerService;
        private ILogger<WorkersController> _logger;

        public WorkersController(IWorkerService workerService, ILogger<WorkersController> logger)
        {
            _workerService = workerService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<ActionResult<ApiResponse<IEnumerable<WorkerDetails>>>> GetAll()
        {
            var workers = await _workerService.GetAllWorkersAsync();
            _logger.LogInformation("Retrieved {WorkerCount} workers", workers.Count());
            return Ok(ApiResponse<IEnumerable<WorkerDetails>>.Success(workers));
        }

        [HttpGet("{workerId}")]
        public async Task<ActionResult<ApiResponse<WorkerDetails?>>> GetById(Guid workerId)
        {
            var worker = await _workerService.GetWorkerByPublicIdAsync(workerId);
            if (worker == null)
            {
                _logger.LogWarning("Worker {WorkerId} not found", workerId);
                return NotFound(ApiResponse<WorkerDetails?>.Fail($"Worker with id '{workerId}' not found"));
            }
            _logger.LogInformation("Retrieved worker {WorkerId}", workerId);
            return Ok(ApiResponse<WorkerDetails?>.Success(worker));
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<RegisterWorkerResponse>>> Register(RegisterWorker registerWorker)
        {
            var worker = await _workerService.RegisterAsync(registerWorker);
            var workerId = worker.WorkerDetails.WorkerId;
            var capabilityCount = registerWorker.JobTypeCapabilities.Count();
            _logger.LogInformation("Worker {WorkerId} registered as {WorkerName} with {CapabilityCount} capabilities",
                workerId, registerWorker.WorkerName, capabilityCount);
            return Ok(ApiResponse<RegisterWorkerResponse>.Success(worker));
        }

        [HttpDelete("{workerId}")]
        public async Task<ActionResult<ApiResponse<WorkerDetails>>> Remove(Guid workerId)
        {
            var worker = await _workerService.RemoveAsync(workerId);
            _logger.LogInformation("Worker {WorkerId} removed", workerId);
            return Ok(ApiResponse<WorkerDetails>.Success(worker));
        }

        [HttpPost("{workerId}/heartbeat")]
        public async Task<ActionResult<ApiResponse<HeartbeatActionStatus>>> HeartBeat(Guid workerId)
        {
            var heartBeatResponse = await _workerService.HeartBeatAsync(workerId);
            _logger.LogInformation("Heartbeat for worker {WorkerId} returned {ActionStatus}",
                workerId, heartBeatResponse.ActionStatus);
            return Ok(ApiResponse<HeartbeatActionStatus>.Success(heartBeatResponse));
        }
    }
}
