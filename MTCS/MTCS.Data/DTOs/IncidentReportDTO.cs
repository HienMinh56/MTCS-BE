using MTCS.Data.DTOs.TripsDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.DTOs.IncidentReportDTO
{
    public class IncidentReportDTO
    {
        public string? ReportId { get; set; }
        public string? TripId { get; set; }
        public string? TrackingCode { get; set; }
        public string? ReportedBy { get; set; }
        public string? IncidentType { get; set; }
        public string? Description { get; set; }
        public DateTime? IncidentTime { get; set; }
        public string? Location { get; set; }
        public int? Type { get; set; }
        public int? VehicleType { get; set; }
        public string? Status { get; set; }
        public decimal? Price { get; set; }
        public int? IsPay { get; set; }
        public string? ResolutionDetails { get; set; }
        public string? HandledBy { get; set; }
        public DateTime? HandledTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<IncidentReportsFileDTO>? IncidentReportsFiles { get; set; }
        public TripDTO? Trip { get; set; }
        public DriverDTO? Driver { get; set; }
        public OrderDTO? Order { get; set; }
    }

    public class IncidentReportsFileDTO
    {
        public string? FileId { get; set; }
        public string? ReportId { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public DateTime? UploadDate { get; set; }
        public string? UploadBy { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public string? FileUrl { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int? Type { get; set; }
    }

    public class TripDTO
    {
        public string? TripId { get; set; }
        public string? OrderId { get; set; }
        public string? DriverId { get; set; }
        public string? TractorId { get; set; }
        public string? TrailerId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public int? MatchType { get; set; }
        public string? MatchBy { get; set; }
        public DateTime? MatchTime { get; set; }
        public string? Note { get; set; }
        public DriverDTO? Driver { get; set; }
        public OrderDTO? Order { get; set; }
    }

    public class DriverDTO
    {
        public string? DriverId { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedBy { get; set; }
        public int? TotalProcessedOrders { get; set; }
    }

    public class OrderDTO
    {
        public string? OrderId { get; set; }
        public string? TrackingCode { get; set; }
        public string? CustomerId { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Weight { get; set; }
        public DateOnly? PickUpDate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public string? Status { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public int? ContainerType { get; set; }
        public string? PickUpLocation { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? ConReturnLocation { get; set; }
        public int? DeliveryType { get; set; }
        public double? Price { get; set; }
        public string? ContainerNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? OrderPlacer { get; set; }
        public decimal? Distance { get; set; }
        public int? ContainerSize { get; set; }
        public int? IsPay { get; set; }
        public TimeOnly? CompletionTime { get; set; }
        public List<TripDTO>? Trips { get; set; }
    }

   
}
