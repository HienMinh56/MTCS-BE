using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class AddIncidentReportImageRequest
    {
        [Required(ErrorMessage = "ReportId is required.")]
        [StringLength(50, ErrorMessage = "ReportId must not exceed 50 characters.")]
        public string ReportId { get; set; }

        public int? ImageType { get; set; }

        [MaxLength(5, ErrorMessage = "A maximum of 5 images are allowed.")]
        public IFormFileCollection? Image { get; set; }
    }
}
