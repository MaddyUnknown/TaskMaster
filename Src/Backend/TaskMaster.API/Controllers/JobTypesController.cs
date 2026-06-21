using Microsoft.AspNetCore.Mvc;
using TaskMaster.API.Interfaces.Services;
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
        public async Task<ActionResult<JobTypeDetails?>> Create(CreateJobType createJob)
        {
            try
            {
                var jobType = await _jobTypeService.CreateJobTypeAsync(createJob);
                _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} created", jobType.Name, jobType.Version);
                return Ok(jobType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job type creation failed for {JobTypeName} v{JobTypeVersion}",
                    createJob.Name, createJob.Version);
                return StatusCode(500);
            }
        }

        [HttpGet("")]
        public async Task<ActionResult<JobTypeDetails>> Get([FromQuery] string name, [FromQuery] long version)
        {
            try
            {
                var jobType = await _jobTypeService.GetJobTypeAsync(new JobTypeRef { Name = name, Version = version });
                if (jobType == null)
                {
                    _logger.LogWarning("Job type {JobTypeName} v{JobTypeVersion} not found", name, version);
                    return NotFound();
                }

                _logger.LogInformation("Job type {JobTypeName} v{JobTypeVersion} retrieved", name, version);
                return Ok(jobType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job type retrieval failed for {JobTypeName} v{JobTypeVersion}", name, version);
                return StatusCode(500);
            }
        }
    }
}
