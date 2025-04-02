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
        public List<string> FileUrls { get; set; } = new List<string>();
    }

}
