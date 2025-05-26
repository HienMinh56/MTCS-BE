using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data.Models;
using Microsoft.AspNetCore.Http;

namespace MTCS.Data.Request
{
    public class CreateExpenseReportRequest
    {
        public string? TripId { get; set; }

        public string? ReportTypeId { get; set; }

        public decimal? Cost { get; set; }

        public string? Location { get; set; }

        public int? IsPay { get; set; }

        public string? Description { get; set; }
        public IFormFileCollection Files { get; set; }
    }
}
