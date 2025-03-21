using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateFuelReportRequest
    {
        public string TripId { get; set; }

        public decimal? RefuelAmount { get; set; }

        public decimal? FuelCost { get; set; }

        public string Location { get; set; }
    }
}
