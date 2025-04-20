using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Response
{
    public class IncidentReportsData
    {
        public string ReportId { get; set; }

        public string TripId { get; set; }

        public string TrackingCode { get; set; }

        public string ReportedBy { get; set; }

        public string IncidentType { get; set; }

        public string Description { get; set; }

        public DateTime IncidentTime { get; set; }

        public string Location { get; set; }

        public int? Type { get; set; }

        public int? VehicleType { get; set; }

        public string Status { get; set; }

        public string ResolutionDetails { get; set; }

        public string HandledBy { get; set; }

        public DateTime? HandledTime { get; set; }

        public DateTime? CreatedDate { get; set; }

        public virtual ICollection<IncidentReportsFile> IncidentReportsFiles { get; set; } = new List<IncidentReportsFile>();

        public virtual Trip Trip { get; set; }
    }
}
