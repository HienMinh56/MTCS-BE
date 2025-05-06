using System.ComponentModel.DataAnnotations;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;

namespace MTCS.Data.DTOs
{
    public class CreateTractorDTO
    {
        [Required(ErrorMessage = "License plate is required")]
        [StringLength(10, MinimumLength = 8, ErrorMessage = "License plate must be between 8 and 10 characters")]
        public required string LicensePlate { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Brand must be between 1 and 20 characters")]
        public required string Brand { get; set; }

        [Range(1990, 2025, ErrorMessage = "Manufacture year must be between 1990 and 2025")]
        public int? ManufactureYear { get; set; }

        [Range(0.1, 100, ErrorMessage = "Max load weight must be between 0 and 100")]
        public decimal? MaxLoadWeight { get; set; }

        [CustomValidation(typeof(DateValidator), "NotFutureDateTime")]
        public DateTime? LastMaintenanceDate { get; set; }

        [Required(ErrorMessage = "Next maintenance date is required")]
        [CustomValidation(typeof(DateValidator), "NotPastDateTime")]
        public DateTime NextMaintenanceDate { get; set; }


        [Required(ErrorMessage = "Registration date is required")]
        [CustomValidation(typeof(DateValidator), "NotFutureDateOnly")]
        public DateOnly? RegistrationDate { get; set; }

        [Required(ErrorMessage = "Registration expiration date is required")]
        [CustomValidation(typeof(DateValidator), "RegistrationExipry")]
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

    public class TractorBasicDTO
    {
        public string TractorId { get; set; }
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public string Status { get; set; }
        public decimal? MaxLoadWeight { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public DateOnly? RegistrationExpirationDate { get; set; }
        public ContainerType? ContainerType { get; set; }
    }

    public class TractorDetailsDTO
    {
        public string TractorId { get; set; }
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public int? ManufactureYear { get; set; }
        public decimal? MaxLoadWeight { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public DateOnly? RegistrationExpirationDate { get; set; }
        public string Status { get; set; }
        public ContainerType? ContainerType { get; set; }
        public int OrderCount { get; set; }
        public DateTime? CreatedDate { get; set; }
        public required string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public List<TractorFileDTO> Files { get; set; } = new List<TractorFileDTO>();
    }

    public class TractorBasicInfoResultDTO
    {
        public PagedList<TractorBasicDTO>? Tractors { get; set; }
        public int AllCount { get; set; }
        public int ActiveCount { get; set; }
        public int MaintenanceDueCount { get; set; }
        public int RegistrationExpiryDueCount { get; set; }
    }


    public class TractorFileDTO
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadBy { get; set; }
    }

    public class TractorUseHistory
    {
        public string TripId { get; set; }
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public string TrailerId { get; set; }
        public string TrailerPlate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public string MatchBy { get; set; }
        public DateTime? MatchTime { get; set; }
    }

    public class TractorUseHistoryPagedDTO
    {
        public PagedList<TractorUseHistory>? TractorUseHistories { get; set; }
    }
}
