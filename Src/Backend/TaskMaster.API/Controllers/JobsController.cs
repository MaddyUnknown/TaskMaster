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
        private ILogger<JobsController> _logger;

        public JobsController(IJobService jobService, ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        [HttpPost("pull")]
        public async Task<ActionResult<JobDetails?>> Pull([FromQuery] Guid workerId)
        {
            try
            {
                var job = await _jobService.GetNextWorkerJobsAsync(workerId);
                _logger.LogInformation("Job pull for worker {WorkerId} returned {JobId}", workerId, job?.JobId);
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job pull failed for worker {WorkerId}", workerId);
                return StatusCode(500);
            }
        }

        [HttpPost("{jobId}/complete")]
        public async Task<ActionResult<JobDetails>> Complete(Guid jobId, WorkerIdRef workerRef)
        {
            try
            {
                var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Completed, workerRef);
                _logger.LogInformation("Job {JobId} completed by worker {WorkerId}", jobId, workerRef.WorkerId);
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId} completion failed for worker {WorkerId}", jobId, workerRef.WorkerId);
                return StatusCode(500);
            }
        }

        [HttpPost("{jobId}/fail")]
        public async Task<ActionResult<JobDetails>> Fail(Guid jobId, WorkerIdRef workerRef)
        {
            try
            {
                var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Failed, workerRef);
                _logger.LogInformation("Job {JobId} failed by worker {WorkerId}", jobId, workerRef.WorkerId);
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobId} fail action failed for worker {WorkerId}", jobId, workerRef.WorkerId);
                return StatusCode(500);
            }
        }

        [HttpPost("")]
        public async Task<ActionResult<JobDetails>> Create(CreateJob jobCreateRequest)
        {
            try
            {
                var job = await _jobService.CreateAsync(jobCreateRequest);
                _logger.LogInformation("Job {JobId} created for job type {JobTypeName} v{JobTypeVersion}",
                    job.JobId, jobCreateRequest.JobType.Name, jobCreateRequest.JobType.Version);
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job creation failed for job type {JobTypeName} v{JobTypeVersion}",
                    jobCreateRequest.JobType.Name, jobCreateRequest.JobType.Version);
                return StatusCode(500);
            }
        }
    }
}
