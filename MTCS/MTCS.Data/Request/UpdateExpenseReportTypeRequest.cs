using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class UpdateExpenseReportTypeRequest
    {
        public string? ReportTypeId { get; set; }
        public string? ReportType { get; set; }

        public int? IsActive { get; set; }
    }
}
