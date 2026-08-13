using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Interfaces.Services
{
    public interface IJobTypeService
    {
        Task<JobTypeDetails> CreateJobTypeAsync(CreateJobType jobType);
        Task<JobTypeDetails?> GetJobTypeAsync(JobTypeRef jobType);
        Task<PagedResult<JobTypeDetails>> GetAllJobTypesAsync(PaginationQuery query);
    }
}
