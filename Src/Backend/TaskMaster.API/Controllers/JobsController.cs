using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Enums;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private IJobService _jobService;
        private ILogger<JobsController> _logger;

        public JobsController(IJobService jobService, ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        [HttpPost("pull")]
        public async Task<ActionResult<ApiResponse<JobDetails?>>> Pull([FromQuery] Guid workerId)
        {
            var job = await _jobService.GetNextWorkerJobsAsync(workerId);
            _logger.LogInformation("Job pull for worker {WorkerId} returned {JobId}", workerId, job?.JobId);
            return Ok(ApiResponse<JobDetails?>.Success(job));
        }

        [HttpPost("{jobId}/complete")]
        public async Task<ActionResult<ApiResponse<JobDetails>>> Complete(Guid jobId, WorkerIdRef workerRef)
        {
            var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Completed, workerRef);
            _logger.LogInformation("Job {JobId} completed by worker {WorkerId}", jobId, workerRef.WorkerId);
            return Ok(ApiResponse<JobDetails>.Success(job));
        }

        [HttpPost("{jobId}/fail")]
        public async Task<ActionResult<ApiResponse<JobDetails>>> Fail(Guid jobId, WorkerIdRef workerRef)
        {
            var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Failed, workerRef);
            _logger.LogInformation("Job {JobId} failed by worker {WorkerId}", jobId, workerRef.WorkerId);
            return Ok(ApiResponse<JobDetails>.Success(job));
        }

        [HttpPost("")]
        public async Task<ActionResult<ApiResponse<JobDetails>>> Create(CreateJob jobCreateRequest)
        {
            var job = await _jobService.CreateAsync(jobCreateRequest);
            _logger.LogInformation("Job {JobId} created for job type {JobTypeName} v{JobTypeVersion}",
                job.JobId, jobCreateRequest.JobType.Name, jobCreateRequest.JobType.Version);
            return Ok(ApiResponse<JobDetails>.Success(job));
        }
    }
}
