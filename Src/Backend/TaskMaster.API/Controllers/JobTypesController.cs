using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Entities;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Controllers
{
    [ApiController]
    [Route("api/job-types")]
    public class JobTypesController : ControllerBase
    {
        private IJobTypeService _jobTypeService;
        private ILogger<JobTypesController> _logger;

        public JobTypesController(IJobTypeService jobTypeService, ILogger<JobTypesController> logger)
        {
            _jobTypeService = jobTypeService;
            _logger = logger;
        }

        [HttpPost("")]
        public async Task<ActionResult<ApiResponse<JobTypeDetails>>> Create(CreateJobType createJob)
        {
            var jobType = await _jobTypeService.CreateJobTypeAsync(createJob);
            _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} created", jobType.Name, jobType.Version);
            return Ok(ApiResponse<JobTypeDetails>.Success(jobType));
        }

        [HttpGet("")]
        public async Task<ActionResult<ApiResponse<JobTypeDetails>>> Get([FromQuery] string name, [FromQuery] long version)
        {
            var jobType = await _jobTypeService.GetJobTypeAsync(new JobTypeRef { Name = name, Version = version });
            if (jobType == null)
            {
                _logger.LogWarning("Job type {JobTypeName} v{JobTypeVersion} not found", name, version);
                throw new NotFoundException(nameof(JobType), (name, version));
            }

            _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} retrieved", name, version);
            return Ok(ApiResponse<JobTypeDetails>.Success(jobType));
        }
    }
}
