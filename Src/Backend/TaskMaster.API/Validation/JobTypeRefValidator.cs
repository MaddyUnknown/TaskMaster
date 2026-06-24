using TaskMaster.API.Constants;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Models.JobTypes;

namespace TaskMaster.API.Validation;

public class JobTypeRefValidator : IValidator<JobTypeRef>
{
    public IReadOnlyList<string> Validate(JobTypeRef instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Name)) errors.Add(ErrorMessage.FieldRequired("Name"));

        if (instance.Version <= 0) errors.Add(ErrorMessage.MustBeGreaterThanZero("Version"));

        return errors;
    }
}
