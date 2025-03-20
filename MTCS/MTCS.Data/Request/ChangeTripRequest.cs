using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class ChangeTripRequest
    {
        public string TripId { get; set; }

        public string OrderId { get; set; }

        public string DriverId { get; set; }

        public string TractorId { get; set; }

        public string TrailerId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string Status { get; set; }

        public decimal? Distance { get; set; }

        public int? MatchType { get; set; }

        public string MatchBy { get; set; }

        public DateTime? MatchTime { get; set; }

    }
}
