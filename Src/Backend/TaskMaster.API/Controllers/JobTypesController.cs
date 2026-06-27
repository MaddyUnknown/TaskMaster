using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Entities;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;
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
        public async Task<ActionResult<ApiResponse<object>>> GetAll([FromQuery] string? name, [FromQuery] long? version)
        {
            if (name != null && version.HasValue)
            {
                var jobTypeRef = new JobTypeRef { Name = name, Version = version.Value };
                var jobType = await _jobTypeService.GetJobTypeAsync(jobTypeRef);
                if (jobType == null)
                {
                    _logger.LogWarning("Job type {JobTypeName} v{JobTypeVersion} not found", name, version);
                    return NotFound(ApiResponse<object>.Fail($"Job type '{name} v{version}' not found"));
                }
                _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} retrieved", name, version);
                return Ok(ApiResponse<object>.Success(jobType));
            }

            var allJobTypes = await _jobTypeService.GetAllJobTypesAsync();
            _logger.LogInformation("Retrieved {JobTypeCount} job types", allJobTypes.Count());
            return Ok(ApiResponse<object>.Success(allJobTypes));
        }
    }
}
