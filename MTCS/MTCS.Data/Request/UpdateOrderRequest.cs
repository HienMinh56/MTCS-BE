using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class UpdateOrderRequest
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public string Note { get; set; }
        public int? Price { get; set; }
    }
}
