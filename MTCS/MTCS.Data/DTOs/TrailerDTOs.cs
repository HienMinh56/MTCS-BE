using MTCS.Data.Helpers;
using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.DTOs
{
    public class CreateTrailerDTO
    {
        [Required(ErrorMessage = "License plate is required")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "License plate must be between 2 and 10 characters")]
        public required string LicensePlate { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Brand must be between 1 and 20 characters")]
        public required string Brand { get; set; }

        [Required(ErrorMessage = "Model is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 20 characters")]
        public required string Model { get; set; }

        [Range(1, 20, ErrorMessage = "Length must be between 0 and 20")]
        public required decimal Length { get; set; }

        [Range(1900, 2025, ErrorMessage = "Manufacture year must be between 1900 and 2100")]
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

        [Required(ErrorMessage = "Trailer category is required")]
        public required string TrailerCateId { get; set; }
    }

    public class TrailerResponseDTO
    {
        public required string TrailerId { get; set; }
        public required string LicensePlate { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public required decimal? Length { get; set; }
        public int? ManufactureYear { get; set; }
        public decimal? MaxLoadWeight { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public DateOnly? RegistrationExpirationDate { get; set; }
        public required string Status { get; set; }
        public required TrailerCategoryResponseDTO Category { get; set; }
    }

    public class TrailerCategoryResponseDTO
    {
        public required string TrailerCateId { get; set; }
        public required string CategoryName { get; set; }
    }
}
