using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateIncidentReportRequest
    {
        [Required(ErrorMessage = "TripId is required.")]
        [StringLength(50, ErrorMessage = "TripId must not exceed 50 characters.")]
        public string TripId { get; set; }

        [StringLength(100, ErrorMessage = "IncidentType must not exceed 100 characters.")]
        public string? IncidentType { get; set; }

        [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
        public string? Description { get; set; }

        [StringLength(200, ErrorMessage = "Location must not exceed 200 characters.")]
        public string? Location { get; set; }

        [Range(1, 2, ErrorMessage = "Type must be between 1 and 2.")]
        public int? Type { get; set; }

        [Range(1, 3, ErrorMessage = "Type must be between 1 and 3.")]
        public List<int>? ImageType { get; set; }

        [RegularExpression("^(Handling|Resolved)$", ErrorMessage = "Status must be Handling or Resolved")]
        public string? Status { get; set; }

        [MaxLength(5, ErrorMessage = "A maximum of 5 images are allowed.")]
        public IFormFileCollection? Image { get; set; }
    }
}