namespace MTCS.Data.DTOs.TripsDTO
{
    public class TripDto
    {
        public string TripId { get; set; }
        public string OrderId { get; set; }
        public string TrackingCode { get; set; }
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public string TractorId { get; set; }
        public string TrailerId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public int? MatchType { get; set; }
        public string MatchBy { get; set; }
        public DateTime? MatchTime { get; set; }
        public string Note { get; set; }
        public List<DeliveryReportDto> DeliveryReports { get; set; }
        public DriverDto Driver { get; set; }
        public List<FuelReportDto> FuelReports { get; set; }
        public List<IncidentReportDto> IncidentReports { get; set; }
        public List<TripStatusHistoryDto> TripStatusHistories { get; set; }
    }

    public class DeliveryReportDto
    {
        public string ReportId { get; set; }
        public string TripId { get; set; }
        public string Notes { get; set; }
        public DateTime? ReportTime { get; set; }
        public string ReportBy { get; set; }
        public List<DeliveryReportFileDto> DeliveryReportsFiles { get; set; }
    }

    public class DeliveryReportFileDto
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
        public DateOnly? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class DriverDto
    {
        public string DriverId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public int? Status { get; set; }
    }

    public class FuelReportDto
    {
        public string ReportId { get; set; }
        public string TripId { get; set; }
        public decimal? RefuelAmount { get; set; }
        public decimal? FuelCost { get; set; }
        public string Location { get; set; }
        public DateTime? ReportTime { get; set; }
        public string ReportBy { get; set; }
        public List<FuelReportFileDto> FuelReportFiles { get; set; }
    }

    public class FuelReportFileDto
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
        public DateOnly? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class IncidentReportDto
    {
        public string ReportId { get; set; }
        public string TripId { get; set; }
        public string ReportedBy { get; set; }
        public string IncidentType { get; set; }
        public string Description { get; set; }
        public DateTime? IncidentTime { get; set; }
        public string Location { get; set; }
        public int? Type { get; set; }
        public string Status { get; set; }
        public string ResolutionDetails { get; set; }
        public string HandledBy { get; set; }
        public DateTime? HandledTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? VehicleType { get; set; }
        public List<IncidentReportFileDto> IncidentReportsFiles { get; set; }
    }

    public class IncidentReportFileDto
    {
        public string FileId { get; set; }
        public string ReportId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime? UploadDate { get; set; }
        public string UploadBy { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string DeletedBy { get; set; }
        public string FileUrl { get; set; }
        public DateOnly? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public int? Type { get; set; }
    }

    public class TripStatusHistoryDto
    {
        public string HistoryId { get; set; }
        public string TripId { get; set; }
        public string StatusId { get; set; }
        public DateTime? StartTime { get; set; }
        public string Status { get; set; }
    }

    public class OrderDto
    {
        public string OrderId { get; set; }
        public string TrackingCode { get; set; }
        public string CustomerId { get; set; }
        public int? Temperature { get; set; }
        public int? Weight { get; set; }
        public DateTime? PickUpDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public int? ContainerType { get; set; }
        public string PickUpLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public string ConReturnLocation { get; set; }
        public int? DeliveryType { get; set; }
        public decimal? Price { get; set; }
        public string ContainerNumber { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public string OrderPlacer { get; set; }
        public decimal? Distance { get; set; }
        public int? ContainerSize { get; set; }
        public bool? IsPay { get; set; }
        public string CompletionTime { get; set; }
    }
}

