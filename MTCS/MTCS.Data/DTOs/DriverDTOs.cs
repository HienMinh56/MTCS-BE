using System.ComponentModel.DataAnnotations;
using MTCS.Data.Helpers;

namespace MTCS.Data.DTOs
{
    public class ViewDriverDTO
    {
        public required string DriverId { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public string? CurrentWeekHours { get; set; }
        public int? TotalOrders { get; set; }
        public int? Status { get; set; }
    }

    public class DriverResponseDTO
    {
        public required string DriverId { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public int? Status { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class DriverProfileDetailsDTO
    {
        public required string DriverId { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public required string PhoneNumber { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public string? DailyWorkingTime { get; set; }
        public string? CurrentWeekWorkingTime { get; set; }
        public int? TotalOrder { get; set; }
        public List<DriverFileDTO> Files { get; set; } = new List<DriverFileDTO>();
    }

    public class DriverFileDTO
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadBy { get; set; }
    }

    public class UpdateDriverDTO
    {
        [Required]
        [MaxLength(25)]
        public required string FullName { get; set; }

        [EmailAddress]
        public required string Email { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [MinLength(6)]
        public string? Password { get; set; }

        [Required]
        [Phone]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0")]
        public required string PhoneNumber { get; set; }
    }

    public class DriverUseHistory
    {
        public string TripId { get; set; }
        public string TractorId { get; set; }
        public string TractorPlate { get; set; }
        public string TrailerId { get; set; }
        public string TrailerPlate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public string MatchBy { get; set; }
        public DateTime? MatchTime { get; set; }
    }

    public class DriverUseHistoryPagedDTO
    {
        public PagedList<DriverUseHistory>? DriverUseHistories { get; set; }
    }

    public class DriverTimeTableResponse
    {
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public List<DriverTimeTable> DriverSchedule { get; set; } = new List<DriverTimeTable>();
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int DeliveringCount { get; set; }
        public int DelayingCount { get; set; }
        public int CanceledCount { get; set; }
        public int NotStartedCount { get; set; }
        public string WeeklyWorkingTime { get; set; }
        public int TotalWeeklyMinutes { get; set; } = 0;
        public string ExpectedWeeklyWorkingTime { get; set; }
        public int ExpectedWeeklyMinutes { get; set; } = 0;
        public List<DailyWorkingTimeDTO> DailyWorkingTimes { get; set; } = new List<DailyWorkingTimeDTO>();
    }

    public class DriverTimeTable
    {
        public string TripId { get; set; }
        public string TrackingCode { get; set; }
        public string OrderDetailId { get; set; }
        public string TractorId { get; set; }
        public string TractorPlate { get; set; }
        public string TrailerId { get; set; }
        public string TrailerPlate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public TimeOnly? EstimatedCompletionTime { get; set; }
    }

    public class DailyWorkingTimeDTO
    {
        public DateOnly Date { get; set; }
        public string WorkingTime { get; set; } = "00:00";
        public int TotalMinutes { get; set; } = 0;
        public string ExpectedWorkingTime { get; set; } = "00:00";
        public int ExpectedMinutes { get; set; } = 0;
    }

}
