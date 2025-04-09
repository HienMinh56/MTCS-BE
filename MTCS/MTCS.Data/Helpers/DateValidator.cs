using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.Helpers
{
    public static class DateValidator
    {
        public static ValidationResult NotFutureDateTime(DateTime? date, ValidationContext context)
        {
            if (!date.HasValue)
                return ValidationResult.Success;

            return date.Value <= DateTime.Now
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

        public static ValidationResult DoB(object value, ValidationContext context)
        {
            const int minAge = 18;

            if (value is not DateOnly birthDate)
                return new ValidationResult("Birth day must be a valid date");

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (birthDate > today)
                return new ValidationResult("Birth date cannot be in the future");

            var age = today.Year - birthDate.Year;

            if (today.Month < birthDate.Month || (today.Month == birthDate.Month && today.Day < birthDate.Day))
                age--;

            return age >= minAge
                ? ValidationResult.Success
                : new ValidationResult($"You must be at least {minAge} years old");
        }

        public static ValidationResult RegistrationExipry(DateOnly? expirationDate, ValidationContext context)
        {

            if (!expirationDate.HasValue)
                return ValidationResult.Success;

            var registrationDateProperty = context.ObjectType.GetProperty("RegistrationDate");
            if (registrationDateProperty == null)
                return ValidationResult.Success;
            var registrationDate = registrationDateProperty.GetValue(context.ObjectInstance) as DateOnly?;

            if (!registrationDate.HasValue)
                return ValidationResult.Success;

            return expirationDate.Value > registrationDate.Value
                ? ValidationResult.Success
                : new ValidationResult("Registration expiration date must be after registration date",
                    new[] { context.MemberName });
        }


    }
}
