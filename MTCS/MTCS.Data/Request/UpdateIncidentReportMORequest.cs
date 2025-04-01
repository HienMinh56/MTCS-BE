using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MTCS.Data.Request
{
    public class UpdateIncidentReportMORequest
    {
        public string ReportId { get; set; }

        public string? IncidentType { get; set; }

        public string? Description { get; set; }

        public string? Location { get; set; }

        public int? Type { get; set; }

        public List<string>? RemovedImage { get; set; } = [];

        public IFormFileCollection? AddedImage { get; set; } = null;
    }
}
