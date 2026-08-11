using TaskMaster.API.Entities;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IJobTypeRepository
    {
        Task<JobType?> GetByJobTypeNameAndVersionAsync(string jobTypeName, long jobTypeVersion);
        Task<IEnumerable<JobType>> GetByJobTypeNameAndVersionAsync(IEnumerable<(string jobTypeName, long jobTypeVersion)> jobTypes);
        Task<PagedResult<JobType>> GetAllJobTypesAsync(PaginationQuery query);
    }
}
