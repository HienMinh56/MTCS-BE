using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.DTOs
{
    public class CategoryCreateDTO
    {
        [Required]
        public required string CategoryId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(30, MinimumLength = 2)]
        public required string CategoryName { get; set; }
    }

    public class CreateTractorDTO
    {
        [Required(ErrorMessage = "License plate is required")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "License plate must be between 2 and 20 characters")]
        public required string LicensePlate { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Brand must be between 1 and 20 characters")]
        public required string Brand { get; set; }

        [Required(ErrorMessage = "Model is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 20 characters")]
        public required string Model { get; set; }

        [Range(1990, 2025, ErrorMessage = "Manufacture year must be between 1990 and 2025")]
        public int? ManufactureYear { get; set; }

        [Range(0, 100, ErrorMessage = "Max load weight must be between 0 and 100")]
        public decimal? MaxLoadWeight { get; set; }

        [CustomValidation(typeof(DateValidator), "NotFutureDateTime")]
        public DateTime LastMaintenanceDate { get; set; }

        public DateTime NextMaintenanceDate { get; set; }

        [Required(ErrorMessage = "Registration date is required")]
        [CustomValidation(typeof(DateValidator), "NotFutureDateOnly")]
        public DateOnly? RegistrationDate { get; set; }

        [Required(ErrorMessage = "Registration expiration date is required")]
        public DateOnly? RegistrationExpirationDate { get; set; }

        [Required(ErrorMessage = "Container type is required")]
        [EnumDataType(typeof(ContainerType), ErrorMessage = "Invalid container type")]
        public int ContainerType { get; set; }
    }

    public class TractorResponseDTO
    {
        public required string TractorId { get; set; }
        public required string LicensePlate { get; set; }
        public required string Brand { get; set; }
        public int? ManufactureYear { get; set; }
        public decimal? MaxLoadWeight { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public DateOnly? RegistrationExpirationDate { get; set; }
        public required string Status { get; set; }
        public ContainerType ContainerType { get; set; }
    }

}
