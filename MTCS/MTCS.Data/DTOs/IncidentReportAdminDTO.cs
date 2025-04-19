using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.DTOs
{
    public class IncidentReportAdminDTO
    {
        public string ReportId { get; set; }
        public string TripId { get; set; }

        public string IncidentType { get; set; }
        public string Description { get; set; }
        public DateTime IncidentTime { get; set; }

        public string Status { get; set; }
        public string? ResolutionDetails { get; set; }
        public string? HandledBy { get; set; }
        public DateTime? HandledTime { get; set; }

        public string ReportedBy { get; set; }
    }
}
