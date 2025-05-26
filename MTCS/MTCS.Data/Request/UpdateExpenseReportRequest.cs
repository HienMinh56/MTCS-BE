using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MTCS.Data.Request
{
    public class UpdateExpenseReportRequest
    {
        public string ReportId { get; set; }

        public string? ReportTypeId { get; set; }

        public decimal? Cost { get; set; }

        public string? Location { get; set; }
        public int? IsPay { get; set; }

        public string? Description { get; set; }
        public List<string> FileIdsToRemove { get; set; } = new();
        public IFormFileCollection? AddedFiles { get; set; } = null;

    }
}
