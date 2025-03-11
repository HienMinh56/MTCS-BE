using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class IncidentReportsFileUpdateRequest
    {
        public string FileId { get; set; }

        public string Description { get; set; }

        public string Note { get; set; }
        public int Type { get; set; }
    }
}
