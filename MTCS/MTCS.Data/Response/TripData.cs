using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Response
{
    public class TripData
    {
        public string TripId { get; set; }

        public string OrderId { get; set; }

        public string TrackingCode { get; set; }

        public string DriverId { get; set; }

        public string TractorId { get; set; }

        public string TrailerId { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string Status { get; set; }

        public int? MatchType { get; set; }

        public string MatchBy { get; set; }

        public DateTime? MatchTime { get; set; }

        public virtual ICollection<DeliveryReport> DeliveryReports { get; set; } = new List<DeliveryReport>();

        public virtual Driver Driver { get; set; }

        public virtual ICollection<FuelReport> FuelReports { get; set; } = new List<FuelReport>();

        public virtual ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();

        public virtual Order Order { get; set; }

        public virtual Tractor Tractor { get; set; }

        public virtual Trailer Trailer { get; set; }

        public virtual ICollection<TripStatusHistory> TripStatusHistories { get; set; } = new List<TripStatusHistory>();
    }
}
