using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.DTOs
{
    public class ViewDriverDTO
    {
        public required string DriverId { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
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
        public int TotalWorkingTime { get; set; }
        public int CurrentWeekWorkingTime { get; set; }
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

        [Required]
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
}
