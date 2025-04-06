using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class UpdateTripRequest
    {
        public string? DriverId { get; set; }
        public string? TractorId { get; set; }
        public string? TrailerId { get; set; }
        public string? Status { get; set; }
    }
}
