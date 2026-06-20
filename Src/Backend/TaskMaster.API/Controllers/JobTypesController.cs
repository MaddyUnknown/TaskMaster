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

        public JobTypesController(IJobTypeService jobTypeService)
        {
            _jobTypeService = jobTypeService;
        }


        [HttpPost("")]
        public async Task<ActionResult<JobTypeDetails?>> Create(CreateJobType createJob)
        {
            var jobType = await _jobTypeService.CreateJobTypeAsync(createJob);
            return Ok(jobType);
        }

        [HttpGet("")]
        public async Task<ActionResult<JobTypeDetails>> Get([FromQuery] string name, [FromQuery] long version)
        {
            var jobType = await _jobTypeService.GetJobTypeAsync(new JobTypeRef { Name = name, Version = version });
            if (jobType == null) return NotFound();

            return Ok(jobType);
        }
    }
}
