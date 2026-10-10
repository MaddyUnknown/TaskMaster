using TaskMaster.API.Constants;
using TaskMaster.API.Interfaces;

namespace TaskMaster.API.Validation
{
    public class TimeZoneValidator : IValidator<string>
    {
        public IReadOnlyList<string> Validate(string timeZone)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(timeZone))
            {
                errors.Add(ErrorMessage.FieldRequired(nameof(timeZone)));
                return errors;
            }

            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                errors.Add(ErrorMessage.InvalidTimeZone(timeZone));
            }
            catch (InvalidTimeZoneException)
            {
                errors.Add(ErrorMessage.InvalidTimeZone(timeZone));
            }

            return errors;
        }
    }
}