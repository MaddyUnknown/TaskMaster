using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private IJobService _jobService;

        public JobsController(IJobService jobService)
        {
            _jobService = jobService;
        }
    

        [HttpPost("pull")]
        public async Task<ActionResult<JobDetails?>> Pull(Guid workerId)
        {
            var job = await _jobService.GetNextWorkerJobsAsync(workerId);
            return Ok(job);
        }

        [HttpPost("{jobId}/complete")]
        public async Task<ActionResult<JobDetails>> Complete(Guid jobId, WorkerIdRef workerRef)
        {
            var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Completed, workerRef);
            return Ok(job);
        }

        [HttpPost("{jobId}/fail")]
        public async Task<ActionResult<JobDetails>> Fail(Guid jobId, WorkerIdRef workerRef)
        {
            var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Failied, workerRef);
            return Ok(job);
        }

        [HttpPost("")]
        public async Task<ActionResult<JobDetails>> Create(JobCreateRequest jobCreateRequest)
        {
            var job = await _jobService.CreateAsync(jobCreateRequest);
            return Ok(job);
        }
    }
}
