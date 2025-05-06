using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Response
{
    public class TripMoResponse
    {
        public string TripId { get; set; }

        public string TrackingCode { get; set; }
        public string ContainerNumber { get; set; }

        public DateOnly? PickUpDate { get; set; }

        public DateOnly? DeliveryDate { get; set; }
        public string PickUpLocation { get; set; }

        public string DeliveryLocation { get; set; }

        public string ConReturnLocation { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string Status { get; set; }
    }
}
