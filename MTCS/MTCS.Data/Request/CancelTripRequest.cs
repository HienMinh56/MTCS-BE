using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CancelTripRequest
    {
        public string TripId { get; set; }

        public string Note { get; set; }
    }
}
