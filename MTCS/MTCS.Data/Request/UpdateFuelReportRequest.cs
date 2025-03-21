using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MTCS.Data.Request
{
    public class UpdateFuelReportRequest
    {
        public string ReportId { get; set; }
        public decimal? RefuelAmount { get; set; }

        public decimal? FuelCost { get; set; }

        public string Location { get; set; }

        public List<string> FileIdsToRemove { get; set; } = new();
        public IFormFileCollection? AddedFiles { get; set; } = null;
    }
}
