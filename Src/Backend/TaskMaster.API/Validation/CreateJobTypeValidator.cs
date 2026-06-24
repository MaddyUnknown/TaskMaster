using TaskMaster.API.Constants;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Validation;

public class CreateJobTypeValidator : IValidator<CreateJobType>
{
    public IReadOnlyList<string> Validate(CreateJobType instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Name)) errors.Add(ErrorMessage.FieldRequired("Name"));

        if (instance.Version <= 0) errors.Add(ErrorMessage.MustBeGreaterThanZero("Version"));

        if (string.IsNullOrWhiteSpace(instance.Schema)) errors.Add(ErrorMessage.FieldRequired("Schema"));

        return errors;
    }
}
