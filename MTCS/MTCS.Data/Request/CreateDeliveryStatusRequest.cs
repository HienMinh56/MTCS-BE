using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreateDeliveryStatusRequest
    {
        public string StatusId { get; set; }
        public string StatusName { get; set; }
        public int? IsActive { get; set; }
        public int? StatusIndex { get; set; }
    }
}
