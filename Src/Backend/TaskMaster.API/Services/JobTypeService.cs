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
        private IJobTypeRepository _jobTypeRepository;

        public JobTypeService(IUnitOfWork unitOfWork, IRepository<JobType> jobTypeCRUDRepo, IJobTypeRepository jobTypeRepository)
        {
            _unitOfWork = unitOfWork;
            _jobTypeCRUDRepo = jobTypeCRUDRepo;
            _jobTypeRepository = jobTypeRepository;
        }

        public async Task<JobTypeDetails> CreateJobTypeAsync(CreateJobType jobType)
        {
            var jobTypeEntity = jobType.ToJobType();
            _jobTypeCRUDRepo.Add(jobTypeEntity);

            await _unitOfWork.SaveAsync();

            return jobTypeEntity.ToJobTypeDetails();
        }

        public async Task<JobTypeDetails?> GetJobTypeAsync(GetJobType jobType)
        {
            var jobTypeEntity = await _jobTypeRepository.GetByJobTypeNameAndVersionAsync(jobType.Name, jobType.Version);
            return jobTypeEntity?.ToJobTypeDetails();
        }
    }
}
