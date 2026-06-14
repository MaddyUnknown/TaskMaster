using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using TaskMaster.Library.Common.Interfaces.HttpClients;
using TaskMaster.Library.Common.Interfaces.Registries;
using TaskMaster.Library.Common.Models.Jobs;
using TaskMaster.Library.Producer.Attributes;
using TaskMaster.Library.Producer.Constants;
using TaskMaster.Library.Producer.Interfaces;

namespace TaskMaster.Library.Producer.Producers
{
    public class TaskProducer : IProducer
    {
        private IJobTypeSchemaRegistry _schemaRegistry;
        private IApiHttpClient _httpClient;

        public TaskProducer()
        {
            _schemaRegistry = TaskMasterProducer.ServiceProducer.GetRequiredService<IJobTypeSchemaRegistry>();
            _httpClient = TaskMasterProducer.ServiceProducer.GetRequiredService<IApiHttpClient>();
        }

        public TaskProducer(IJobTypeSchemaRegistry schemaRegistry, IApiHttpClient httpClient)
        {
            _schemaRegistry = schemaRegistry;
            _httpClient = httpClient;
        }

        public async Task ProduceAsync<T>(T data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var jobType = GetJobType(data.GetType());

            var schema = await _schemaRegistry.GetByJobTypeNameAndVersion(jobType.Name, jobType.Version);
            var payload = JsonConvert.SerializeObject(data);

            var validationErrors = ValidatePayload(payload, schema);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(ErrorMessage.JsonSchemaValidationFailed(validationErrors));
            }

            await _httpClient.CreateJob(new CreateJobRequest
            {
                JobType = new JobTypeRef
                {
                    Name = jobType.Name,
                    Version = jobType.Version
                },
                Payload = payload
            });
        }

        private static JobTypeAttribute GetJobType(Type payloadType)
        {
            var attribute = payloadType.GetCustomAttributes(typeof(JobTypeAttribute), inherit: false)
                .OfType<JobTypeAttribute>()
                .FirstOrDefault();

            if (attribute == null)
            {
                throw new InvalidOperationException(ErrorMessage.JobTypeAttributeNotFound(payloadType.FullName ?? string.Empty));
            }

            return attribute;
        }

        private static IList<string> ValidatePayload(string payload, string schema)
        {
            var json = JToken.Parse(payload);
            var jsonSchema = JSchema.Parse(schema);

            json.IsValid(jsonSchema, out IList<string> validationErrors);
            return validationErrors;
        }
    }
}
