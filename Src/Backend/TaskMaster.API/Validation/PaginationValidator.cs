using TaskMaster.API.Constants;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Validation;

public static class PaginationValidator
{
    public const int MaxPageSize = 100;

    public static IReadOnlyList<string> Validate(PaginationQuery query)
    {
        var errors = new List<string>();

        if (query.Page.HasValue && query.Page.Value < 1) errors.Add(ErrorMessage.MustBeGreaterThanZero("Page"));
        if (query.PageSize.HasValue && query.PageSize.Value < 1) errors.Add(ErrorMessage.MustBeGreaterThanZero("PageSize"));
        if (query.PageSize.HasValue && query.PageSize.Value > MaxPageSize) errors.Add(ErrorMessage.MustBeLessThanOrEqualTo("PageSize", MaxPageSize));
        if ((query.Page.HasValue && !query.PageSize.HasValue) || (!query.Page.HasValue && query.PageSize.HasValue)) errors.Add(ErrorMessage.PageAndPageSizeProvidedTogether());

        return errors;
    }
}
