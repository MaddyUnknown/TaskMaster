using TaskMaster.API.Constants;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Models.Jobs;

namespace TaskMaster.API.Validation;

public class CreateJobValidator : IValidator<CreateJob>
{
    public IReadOnlyList<string> Validate(CreateJob instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.JobType.Name)) errors.Add(ErrorMessage.FieldRequired("JobType.Name"));

        if (instance.JobType.Version <= 0) errors.Add(ErrorMessage.MustBeGreaterThanZero("JobType.Version"));

        if (string.IsNullOrWhiteSpace(instance.Payload)) errors.Add(ErrorMessage.FieldRequired("Payload"));

        return errors;
    }
}
