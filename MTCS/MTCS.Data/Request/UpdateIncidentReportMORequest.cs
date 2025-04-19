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
        [Required(ErrorMessage = "ReportId is required.")]
        [StringLength(50, ErrorMessage = "ReportId must not exceed 50 characters.")]
        public string ReportId { get; set; }

        [StringLength(100, ErrorMessage = "IncidentType must not exceed 100 characters.")]
        public string? IncidentType { get; set; }

        [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "Location must not exceed 200 characters.")]
        public string? Location { get; set; }

        [Range(1, 2, ErrorMessage = "Type must be between 1 and 2.")]
        public int? Type { get; set; }

        [Range(1, 2, ErrorMessage = "Type must be between 1 and 2.")]
        public int? VehicleType { get; set; }

        public List<string>? RemovedImage { get; set; } = [];

        public IFormFileCollection? AddedImage { get; set; } = null;
    }
}
