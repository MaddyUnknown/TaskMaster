using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Constants;
using TaskMaster.Library.Common.Interfaces.Caches;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.JobType;

namespace TaskMaster.Library.Common.Registries
{
    internal class JobTypeSchemaRegistry : IJobTypeSchemaRegistry
    {
        private IApiHttpClient _httpClient;
        private ICache _cache;

        public JobTypeSchemaRegistry(IApiHttpClient apiHttpClient, ICache cache)
        {
            _httpClient = apiHttpClient;
            _cache = cache;
        }

        public async Task<string> GetByJobTypeNameAndVersion(string jobTypeName, long jobTypeVersion)
        {
            var schema = await _cache.GetOrAdd(CacheKey.JobType(jobTypeName, jobTypeVersion), async () =>
            {
                var request = new GetJobTypeRequest { JobTypeName = jobTypeName, JobTypeVersion = jobTypeVersion };
                var response = await _httpClient.GetJobType(request);
                return response?.Schema;
            });

            if (schema == null) throw new Exception(ErrorMessage.JobTypeNotFound(jobTypeName, jobTypeVersion));
            return schema;
        }
    }
}
