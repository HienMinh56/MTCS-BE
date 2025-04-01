using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class ResolvedIncidentReportRequest
    {
        public string reportId { get; set; }
        public string ResolutionDetails { get; set; }
    }
}
