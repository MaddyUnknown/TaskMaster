using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Interfaces.Services;
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

        [HttpPost("register")]
        public async Task<ActionResult<RegisterWorkerResponse>> Register(RegisterWorker registerWorker)
        {
            try
            {
                var worker = await _workerService.RegisterAsync(registerWorker);
                var workerId = worker.WorkerDetails.WorkerId;
                var capabilityCount = registerWorker.JobTypeCapabilities.Count();
                _logger.LogInformation("Worker {WorkerId} registered as {WorkerName} with {CapabilityCount} capabilities",
                    workerId, registerWorker.WorkerName, capabilityCount);
                return Ok(worker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker registration failed for {WorkerName}", registerWorker.WorkerName);
                return StatusCode(500);
            }
        }

        [HttpDelete("{workerId}")]
        public async Task<ActionResult<WorkerDetails>> Remove(Guid workerId)
        {
            try
            {
                var worker = await _workerService.RemoveAsync(workerId);
                _logger.LogInformation("Worker {WorkerId} removed", workerId);
                return Ok(worker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker removal failed for {WorkerId}", workerId);
                return StatusCode(500);
            }
        }

        [HttpPost("{workerId}/heartbeat")]
        public async Task<ActionResult<HeartbeatActionStatus>> HeartBeat(Guid workerId)
        {
            try
            {
                var heartBeatResponse = await _workerService.HeartBeatAsync(workerId);
                _logger.LogInformation("Heartbeat for worker {WorkerId} returned {ActionStatus}",
                    workerId, heartBeatResponse.ActionStatus);
                return Ok(heartBeatResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat failed for worker {WorkerId}", workerId);
                return StatusCode(500);
            }
        }
    }
}
