using TaskMaster.API.Entities;

namespace TaskMaster.API.Interfaces.Repositories
{
    public interface IJobTypeRepository
    {
        Task<JobType?> GetByJobTypeNameAndVersionAsync(string jobTypeName, long jobTypeVersion);
        Task<IEnumerable<JobType>> GetByJobTypeNameAndVersionAsync(IEnumerable<(string jobTypeName, long jobTypeVersion)> jobTypes);
    }
}
