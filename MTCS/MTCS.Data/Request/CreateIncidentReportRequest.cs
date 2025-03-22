using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateIncidentReportRequest
    {
        [Required(ErrorMessage = "ReportId is required.")]
        [StringLength(50, ErrorMessage = "ReportId must not exceed 50 characters.")]        
        public string ReportId { get; set; }

        [Required(ErrorMessage = "TripId is required.")]
        public string TripId { get; set; }

        [Required(ErrorMessage = "ReportedBy is required.")]
        public string ReportedBy { get; set; }

        [Required(ErrorMessage = "IncidentType is required.")]
        [StringLength(100, ErrorMessage = "IncidentType must not exceed 100 characters.")]
        public string IncidentType { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "IncidentTime is required.")]
        [DataType(DataType.DateTime, ErrorMessage = "Invalid date format.")]
        public DateTime IncidentTime { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200, ErrorMessage = "Location must not exceed 200 characters.")]
        public string Location { get; set; }

        [Range(1, 5, ErrorMessage = "Type must be between 1 and 5.")]
        public int? Type { get; set; }

        [Required(ErrorMessage = "ImageType is required.")]
        [MinLength(1, ErrorMessage = "At least one ImageType is required.")]
        public List<int> ImageType { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Handling|Resolved)$", ErrorMessage = "Status must be InProgress, Resolved")]
        public string Status { get; set; }

        public DateTime? CreatedDate { get; set; }

        [Required(ErrorMessage = "At least one image is required.")]
        [MaxLength(5, ErrorMessage = "A maximum of 5 images are allowed.")]
        public IFormFileCollection? Image { get; set; }
    }
}