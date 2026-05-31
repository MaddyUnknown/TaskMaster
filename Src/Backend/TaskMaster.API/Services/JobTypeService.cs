using TaskMaster.API.Entities;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Mappers;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Services
{
    public class JobTypeService : IJobTypeService
    {
        private IUnitOfWork _unitOfWork;
        private IRepository<JobType> _jobTypeCRUDRepo;

        public JobTypeService(IUnitOfWork unitOfWork, IRepository<JobType> jobTypeCRUDRepo)
        {
            _unitOfWork = unitOfWork;
            _jobTypeCRUDRepo = jobTypeCRUDRepo;
        }

        public async Task<JobTypeDetails> CreateJobTypeAsync(CreateJobType jobType)
        {
            var jobTypeEntity = jobType.ToJobType();
            _jobTypeCRUDRepo.Add(jobTypeEntity);

            await _unitOfWork.SaveAsync();

            return jobTypeEntity.ToJobTypeDetails();
        }
    }
}
