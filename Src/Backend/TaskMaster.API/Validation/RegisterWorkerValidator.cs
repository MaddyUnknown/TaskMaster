using TaskMaster.API.Constants;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Validation;

public class RegisterWorkerValidator : IValidator<RegisterWorker>
{
    public IReadOnlyList<string> Validate(RegisterWorker instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.WorkerName)) errors.Add(ErrorMessage.FieldRequired("WorkerName"));

        if (instance.JobTypeCapabilities == null)
        {
            errors.Add(ErrorMessage.FieldRequired("JobTypeCapabilities"));
        }
        else
        {
            var capabilities = instance.JobTypeCapabilities.ToList();
            if (capabilities.Count == 0)
            {
                errors.Add(ErrorMessage.AtLeastOneJobTypeCapability());
            }
            else
            {
                for (var i = 0; i < capabilities.Count; i++)
                {
                    var cap = capabilities[i];
                    if (string.IsNullOrWhiteSpace(cap.Name)) errors.Add(ErrorMessage.CapabilityFieldRequired(i, "Name"));
                    if (cap.Version <= 0) errors.Add(ErrorMessage.CapabilityMustBeGreaterThanZero(i, "Version"));
                }
            }
        }

        return errors;
    }
}
