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

        public string TrackingCode { get; set; }

        public string DriverName { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string Status { get; set; }

    }
}
