using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Auth;
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
        [RequirePermission(AuthPermissions.CreateJobTypes)]
        public async Task<ActionResult<ApiResponse<JobTypeDetails>>> Create(CreateJobType createJob)
        {
            var jobType = await _jobTypeService.CreateJobTypeAsync(createJob);
            _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} created", jobType.Name, jobType.Version);
            return Ok(ApiResponse<JobTypeDetails>.Success(jobType));
        }

        [HttpGet("")]
        [RequirePermission(AuthPermissions.ReadJobTypes)]
        public async Task<ActionResult<ApiResponse<IEnumerable<JobTypeDetails>>>> GetAll([FromQuery] string? name, [FromQuery] long? version, [FromQuery] PaginationQuery query)
        {
            if (name != null && version.HasValue)
            {
                var jobTypeRef = new JobTypeRef { Name = name, Version = version.Value };
                var jobType = await _jobTypeService.GetJobTypeAsync(jobTypeRef);
                var result = jobType != null ? [jobType] : Enumerable.Empty<JobTypeDetails>();
                _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} lookup returned {Count} result(s)", name, version, result.Count());
                return Ok(ApiResponse<IEnumerable<JobTypeDetails>>.Success(result));
            }

            var allJobTypes = await _jobTypeService.GetAllJobTypesAsync(query);
            _logger.LogInformation("Retrieved {JobTypeCount} job types (page {Page}/{TotalPages})", allJobTypes.TotalCount, allJobTypes.Page, allJobTypes.TotalPages);
            return Ok(ApiResponse<PagedResult<JobTypeDetails>>.Success(allJobTypes));
        }
    }
}
