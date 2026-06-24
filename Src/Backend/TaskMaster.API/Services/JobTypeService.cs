using TaskMaster.API.Entities;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
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
        private IValidator<CreateJobType> _createJobTypeValidator;
        private IValidator<JobTypeRef> _jobTypeRefValidator;

        public JobTypeService(IUnitOfWork unitOfWork, IRepository<JobType> jobTypeCRUDRepo, IJobTypeRepository jobTypeRepository, IValidator<CreateJobType> createJobTypeValidator, IValidator<JobTypeRef> jobTypeRefValidator)
        {
            _unitOfWork = unitOfWork;
            _jobTypeCRUDRepo = jobTypeCRUDRepo;
            _jobTypeRepository = jobTypeRepository;
            _createJobTypeValidator = createJobTypeValidator;
            _jobTypeRefValidator = jobTypeRefValidator;
        }

        public async Task<JobTypeDetails> CreateJobTypeAsync(CreateJobType jobType)
        {
            var errors = _createJobTypeValidator.Validate(jobType);
            if (errors.Count > 0) throw new ValidationException(errors);

            var jobTypeEntity = jobType.ToJobType();
            _jobTypeCRUDRepo.Add(jobTypeEntity);

            await _unitOfWork.SaveAsync();

            return jobTypeEntity.ToJobTypeDetails();
        }

        public async Task<JobTypeDetails?> GetJobTypeAsync(JobTypeRef jobType)
        {
            var errors = _jobTypeRefValidator.Validate(jobType);
            if (errors.Count > 0) throw new ValidationException(errors);

            var jobTypeEntity = await _jobTypeRepository.GetByJobTypeNameAndVersionAsync(jobType.Name, jobType.Version);
            return jobTypeEntity?.ToJobTypeDetails();
        }
    }
}
