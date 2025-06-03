namespace MTCS.Data.DTOs
{
    public class ExpenseReportDTO
    {
        public string ReportId { get; set; }
        public string TripId { get; set; }
        public string ReportTypeId { get; set; }
        public string ReportTypeName { get; set; }
        public decimal? Cost { get; set; }
        public string Location { get; set; }
        public DateTime? ReportTime { get; set; }
        public string ReportBy { get; set; }
        public int? IsPay { get; set; }
        public string Description { get; set; }

        // Trip related info
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public string OrderDetailId { get; set; }
        public string TrackingCode { get; set; }

        // Files
        public List<ExpenseReportFileDTO> ExpenseReportFiles { get; set; } = new List<ExpenseReportFileDTO>();
    }

    public class ExpenseReportFileDTO
    {
        public string FileId { get; set; }
        public string ReportId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime? UploadDate { get; set; }
        public string UploadBy { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public string FileUrl { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedBy { get; set; }
    }
}
