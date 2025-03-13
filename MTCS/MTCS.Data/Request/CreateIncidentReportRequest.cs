using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateIncidentReportRequest
    {
        public string ReportId { get; set; }

        public string TripId { get; set; }

        public string ReportedBy { get; set; }

        public string IncidentType { get; set; }

        public string Description { get; set; }

        public DateTime IncidentTime { get; set; }

        public string Location { get; set; }

        public int? Type { get; set; }

        public List<int> ImageType { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedDate { get; set; }

        public IFormFileCollection? Image { get; set; }
    }
}