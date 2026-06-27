using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskMaster.API.Data;
using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Repositories;
using static Azure.Core.HttpHeader;

namespace TaskMaster.API.Repositories
{
    public class JobTypeRepository : IJobTypeRepository
    {
        private ApplicationDbContext _context;

        public JobTypeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JobType?> GetByJobTypeNameAndVersionAsync(string jobTypeName, long jobTypeVersion)
        {
            return await _context.JobTypes.Where(j => j.Name == jobTypeName && j.Version == jobTypeVersion).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<JobType>> GetByJobTypeNameAndVersionAsync(IEnumerable<(string jobTypeName, long jobTypeVersion)> jobTypes)
        {
            var requested = jobTypes.ToHashSet();
            var names = requested.Select(x => x.jobTypeName).Distinct().ToList();

            var candidates = await _context.JobTypes.Where(j => names.Contains(j.Name)).ToListAsync();
            return candidates.Where(j => requested.Contains((j.Name, j.Version)));
        }

        public async Task<IEnumerable<JobType>> GetAllJobTypesAsync()
        {
            return await _context.JobTypes.OrderByDescending(j => j.Id).ToListAsync();
        }
    }
}
