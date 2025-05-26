using MTCS.Data.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.DTOs
{

    public class OrderDto
    {
        public string OrderId { get; set; }
        public string TrackingCode { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; } = new();
    }
    public class OrderDetailDto
    {
        public string OrderDetailId { get; set; }
        public string OrderId { get; set; }
        public DateOnly? PickUpDate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public string Status { get; set; }
        public string PickUpLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public List<TripDto> Trips { get; set; } = new();
    }

    public class OrderDetailFileDto
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    public class TripDto
    {
        public string TripId { get; set; }
        public string OrderDetailId { get; set; }
        public string DriverId { get; set; }
        public string TractorId { get; set; }
        public string TrailerId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public DateTime? MatchTime { get; set; }
        public DriverDto Driver { get; set; }
        public TractorDto Tractor { get; set; }
        public TrailerDto Trailer { get; set; }
        public List<TripStatusHistoryDto> TripStatusHistories { get; set; }
    }

    public class TripStatusHistoryDto
    {
        public string HistoryId { get; set; }
        public string TripId { get; set; }
        public string StatusId { get; set; }
        public string StatusName { get; set; }
        public DateTime? StartTime { get; set; }
    }

    public class DriverDto
    {
        public string DriverId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class TractorDto
    {
        public string TractorId { get; set; }
        public string LicensePlate { get; set; }
    }

    public class TrailerDto
    {
        public string TrailerId { get; set; }
        public string LicensePlate { get; set; }
    }

    public class CustomerDto
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }      
    }
}
