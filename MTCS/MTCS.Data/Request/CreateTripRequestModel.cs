using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateTripRequestModel
    {
        public string OrderId { get; set; }

        public string DriverId { get; set; }

        public string TractorId { get; set; }

        public string TrailerId { get; set; }

    }
}
