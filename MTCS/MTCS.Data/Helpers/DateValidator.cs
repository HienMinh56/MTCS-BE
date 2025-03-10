using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.Helpers
{
    public static class DateValidator
    {
        public static ValidationResult NotFutureDateTime(DateTime date, ValidationContext context)
        {
            return date <= DateTime.Now
                ? ValidationResult.Success
                : new ValidationResult("Date cannot be in the future");
        }

        public static ValidationResult NotFutureDateOnly(DateOnly? date, ValidationContext context)
        {
            if (!date.HasValue)
                return ValidationResult.Success;

            return date.Value <= DateOnly.FromDateTime(DateTime.Today)
                ? ValidationResult.Success
                : new ValidationResult("Date cannot be in the future");
        }
    }
}
