namespace MTCS.Data.DTOs
{
    public class TripDTO
    {
        public required string TripId { get; set; }
        public required string OrderId { get; set; }
        public required string TrackingCode { get; set; }
        public required string DriverName { get; set; }
        public required string CurrentStatus { get; set; }
    }


    public class DetailedTripDTO
    {
        public required string TripId { get; set; }
        public string? DriverName { get; set; }
        public string? CurrentStatus { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal? Distance { get; set; }
        public string? MatchBy { get; set; }

        // Related entities information
        public VehicleInfoDTO? TractorInfo { get; set; }
        public VehicleInfoDTO? TrailerInfo { get; set; }
        public OrderInfoDTO? OrderInfo { get; set; }

        // Status history
        public List<StatusHistoryDTO>? StatusHistory { get; set; }

        // Reports counts
        public int DeliveryReportsCount { get; set; }
        public int FuelReportsCount { get; set; }
        public int IncidentReportsCount { get; set; }
        public int InspectionLogsCount { get; set; }
    }

    public class VehicleInfoDTO
    {
        public string? Id { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? LicensePlate { get; set; }
    }

    public class OrderInfoDTO
    {
        public string? OrderId { get; set; }
        public required string TrackingCode { get; set; }
        public string? PickupLocation { get; set; }
        public string? DeliveryLocation { get; set; }
        public DateOnly? PickupDate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Temperature { get; set; }
        public string? Status { get; set; }
    }

    public class StatusHistoryDTO
    {
        public string? StatusName { get; set; }
        public DateTime ChangeTime { get; set; }
        public string? Notes { get; set; }
    }
}

