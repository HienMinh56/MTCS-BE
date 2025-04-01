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
        public string? Note { get; set; }
        public int? ContainerType { get; set; }
        public string? PickUpLocation { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? ConReturnLocation { get; set; }
        public int? DeliveryType { get; set; }
        public int? Price { get; set; }
        public string? ContainerNumber { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? OrderPlacer { get; set; }
        public decimal? Distance { get; set; }
    }
}
