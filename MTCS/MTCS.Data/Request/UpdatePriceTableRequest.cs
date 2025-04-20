using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.Request
{
    public class UpdatePriceTableRequest
    {
        [Required(ErrorMessage = "PriceId is required")]
        public string PriceId { get; set; }

        [Range(0.01, 10000000, ErrorMessage = "MinPricePerKm must be greater than 0 and less than 10,000,000")]
        public decimal? MinPricePerKm { get; set; }

        [Range(0.01, 10000000, ErrorMessage = "MaxPricePerKm must be greater than 0 and less than 10,000,000")]
        public decimal? MaxPricePerKm { get; set; }

        [CustomValidation(typeof(UpdatePriceTableRequest), nameof(ValidateMinMaxPrice))]
        public decimal? ValidationProperty { get; set; }

        public static ValidationResult ValidateMinMaxPrice(object value, ValidationContext context)
        {
            var instance = (UpdatePriceTableRequest)context.ObjectInstance;

            if (instance.MinPricePerKm.HasValue && instance.MaxPricePerKm.HasValue)
            {
                if (instance.MinPricePerKm > instance.MaxPricePerKm)
                {
                    return new ValidationResult("MinPricePerKm must be smaller than MaxPricePerKm");
                }
            }

            return ValidationResult.Success;
        }
    }
}
