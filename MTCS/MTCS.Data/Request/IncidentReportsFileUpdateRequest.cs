using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class IncidentReportsFileUpdateRequest
    {
        [Required(ErrorMessage = "FileId is required.")]
        [StringLength(50, ErrorMessage = "FileId must not exceed 50 characters.")]
        public string FileId { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
        public string Description { get; set; }

        [StringLength(200, ErrorMessage = "Note must not exceed 200 characters.")]
        public string Note { get; set; }

        [Range(1, 5, ErrorMessage = "Type must be between 1 and 5.")]
        public int Type { get; set; }
    }
}
