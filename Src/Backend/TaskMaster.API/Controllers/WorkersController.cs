using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TaskMaster.Interfaces.Services;
using TaskMaster.Models.Workers;

namespace TaskMaster.Controllers
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
        public async Task<ActionResult<RegisterWorkerResponse>> Register(RegisterWorkerRequest registerWorker)
        {
            var worker = await _workerService.RegisterAsync(registerWorker);
            return Ok(worker);
        }

        [HttpDelete("{workerId}")]
        public async Task<ActionResult<WorkerDetails>> Remove(Guid workerId)
        {
            var worker = await _workerService.RemoveAsync(workerId);
            return Ok(worker);
        }

        [HttpPost("{workerId}/heartbeat")]
        public async Task<ActionResult<ActionStatusResponse>> HeartBeat(Guid workerId)
        {
            try
            {
                var heartBeatResponse = await _workerService.HeartBeatAsync(workerId);
                return Ok(heartBeatResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while calling heartbeat for workerId: {0} | Exception: {1}", workerId, ex);
                throw;
            }
        }
    }
}
