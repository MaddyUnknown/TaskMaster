using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using TaskMaster.API.Auth;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Exceptions;
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

        [HttpGet("")]
        [RequirePermission(AuthPermissions.ReadJobs)]
        public async Task<ActionResult<ApiResponse<PagedResult<JobDetails>>>> GetAll([FromQuery] JobQuery query)
        {
            var jobs = await _jobService.GetAllJobsAsync(query);
            _logger.LogInformation("Retrieved {JobCount} jobs (page {Page}/{TotalPages})", jobs.Items.Count, jobs.Page, jobs.TotalPages);
            return Ok(ApiResponse<PagedResult<JobDetails>>.Success(jobs));
        }

        [HttpGet("counts")]
        [RequirePermission(AuthPermissions.ReadJobStats)]
        public async Task<ActionResult<ApiResponse<JobStatusCounts>>> GetCounts()
        {
            var counts = await _jobService.GetJobStatusCountsAsync();
            _logger.LogInformation("Retrieved job status counts (queued {Queued}, in-progress {InProgress}, completed {Completed}, failed {Failed})", counts.Queued, counts.InProgress, counts.Completed, counts.Failed);
            return Ok(ApiResponse<JobStatusCounts>.Success(counts));
        }

        [HttpGet("{jobId}")]
        [RequirePermission(AuthPermissions.ReadJobs)]
        public async Task<ActionResult<ApiResponse<JobDetails?>>> GetById(Guid jobId)
        {
            var job = await _jobService.GetJobByPublicIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found", jobId);
                return NotFound(ApiResponse<JobDetails?>.Fail($"Job with id '{jobId}' not found"));
            }
            _logger.LogInformation("Retrieved job {JobId}", jobId);
            return Ok(ApiResponse<JobDetails?>.Success(job));
        }

        [HttpPost("pull")]
        [RequirePermission(AuthPermissions.PullJobs)]
        public async Task<ActionResult<ApiResponse<IEnumerable<JobDetails>>>> Pull([FromQuery] Guid workerId, [FromQuery] int maxJobs = 1)
        {
            var jobs = (await _jobService.GetNextWorkerJobsAsync(workerId, maxJobs)) ?? Enumerable.Empty<JobDetails>();
            _logger.LogInformation("Job pull for worker {WorkerId} returned {JobCount} jobs", workerId, jobs.Count());
            return Ok(ApiResponse<IEnumerable<JobDetails>>.Success(jobs));
        }

        [HttpPost("{jobId}/complete")]
        [RequirePermission(AuthPermissions.ReportJobs)]
        public async Task<ActionResult<ApiResponse<JobDetails>>> Complete(Guid jobId, WorkerIdRef workerRef)
        {
            var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Completed, workerRef);
            _logger.LogInformation("Job {JobId} completed by worker {WorkerId}", jobId, workerRef.WorkerId);
            return Ok(ApiResponse<JobDetails>.Success(job));
        }

        [HttpPost("{jobId}/fail")]
        [RequirePermission(AuthPermissions.ReportJobs)]
        public async Task<ActionResult<ApiResponse<JobDetails>>> Fail(Guid jobId, WorkerIdRef workerRef)
        {
            var job = await _jobService.ChangeJobStatusAsync(jobId, JobStatusEnum.Failed, workerRef);
            _logger.LogInformation("Job {JobId} failed by worker {WorkerId}", jobId, workerRef.WorkerId);
            return Ok(ApiResponse<JobDetails>.Success(job));
        }

        [HttpPost("status/bulk")]
        [RequirePermission(AuthPermissions.ReportJobs)]
        public async Task<ActionResult<ApiResponse<BulkUpdateJobStatusResponse>>> BulkUpdateJobStatus(BulkUpdateJobStatus request)
        {
            var result = await _jobService.ChangeJobStatusAsync(request);
            _logger.LogInformation("Batch result for worker {WorkerId}: {UpdatedCount} jobs updated", request.WorkerId, result.UpdatedRecordCount);
            return Ok(ApiResponse<BulkUpdateJobStatusResponse>.Success(result));
        }

        [HttpPost("")]
        [RequirePermission(AuthPermissions.CreateJob)]
        public async Task<ActionResult<ApiResponse<JobDetails>>> Create(CreateJob jobCreateRequest)
        {
            var job = await _jobService.CreateAsync(jobCreateRequest);
            _logger.LogInformation("Job {JobId} created for job type {JobTypeName} v{JobTypeVersion}", job.JobId, jobCreateRequest.JobType.Name, jobCreateRequest.JobType.Version);
            return Ok(ApiResponse<JobDetails>.Success(job));
        }
    }
}
