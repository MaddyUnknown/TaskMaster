namespace TaskMaster.API.Constants;

internal static class ErrorMessage
{
    internal static string FieldRequired(string fieldName) => $"{fieldName} is required.";
    internal static string MustBeGreaterThanZero(string fieldName) => $"{fieldName} must be greater than 0.";
    internal static string MustBeLessThanOrEqualTo(string fieldName, int maxValue) => $"{fieldName} must be less than or equal to {maxValue}.";
    internal static string AtLeastOneJobTypeCapability() => "At least one job type capability is required.";
    internal static string CapabilityFieldRequired(int index, string fieldName) => $"Job type capability at index {index}: {fieldName} is required.";
    internal static string CapabilityMustBeGreaterThanZero(int index, string fieldName) => $"Job type capability at index {index}: {fieldName} must be greater than 0.";
    internal static string OneOrMoreJobTypeCapabilitiesDoNotExist() => "One or more job type capabilities do not exist.";
    internal static string ActiveWorkerAlreadyExists(string workerName) => $"Active worker with name '{workerName}' already exists";
    internal static string JobNotFound() => $"Job not found";
    internal static string PageAndPageSizeProvidedTogether() => $"Page and PageSize must either both be provided or both be omitted.";
}
