using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class UpdateIncidentReportRequest
    {
        [Required(ErrorMessage = "ReportId is required.")]
        [StringLength(50, ErrorMessage = "ReportId must not exceed 50 characters.")]
        public string ReportId { get; set; }

        [Required(ErrorMessage = "TripId is required.")]
        [StringLength(50, ErrorMessage = "TripId must not exceed 50 characters.")]
        public string TripId { get; set; }

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

        public List<int>? ImageType { get; set; }

        [StringLength(1000, ErrorMessage = "ResolutionDetails must not exceed 1000 characters.")]
        public string? ResolutionDetails { get; set; }

        [RegularExpression("^(Handling|Resolved)$", ErrorMessage = "Status must be Handling or Resolved")]
        public string? Status { get; set; }

        [StringLength(100, ErrorMessage = "HandledBy must not exceed 100 characters.")]
        public string? HandledBy { get; set; }

        [DataType(DataType.DateTime, ErrorMessage = "Invalid date format.")]
        public DateTime? HandledTime { get; set; }

        public List<string>? RemovedImage { get; set; } = [];

        public IFormFileCollection? AddedImage { get; set; } = null;
    }
}