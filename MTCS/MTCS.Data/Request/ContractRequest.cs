using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class ContractRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Summary { get; set; }
        public string SignedBy { get; set; }
        public DateOnly SignedTime { get; set; }
        public int Status { get; set; }
    }
}
