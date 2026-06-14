using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Models.JobType;
using TaskMaster.Library.Common.Models.Jobs;

namespace TaskMaster.Library.Common.Interfaces.HttpClients
{
    public interface IApiHttpClient
    {
        Task<GetJobTypeResponse?> GetJobType(GetJobTypeRequest request);
        Task<CreateJobResponse> CreateJob(CreateJobRequest request);
    }
}
