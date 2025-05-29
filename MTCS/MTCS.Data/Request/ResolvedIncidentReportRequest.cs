using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MTCS.Data.Request
{
    public class ResolvedIncidentReportRequest
    {
        public string reportId { get; set; }
        public string ResolutionDetails { get; set; }
        public decimal? Price { get; set; }
        public List<IFormFile>? BillingImages { get; set; }
    }
}
