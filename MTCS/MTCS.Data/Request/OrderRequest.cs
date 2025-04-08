using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class OrderRequest
    {
        public string CompanyName { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Weight { get; set; }
        public DateOnly? PickUpDate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public string? Note { get; set; }
        public int? ContainerType { get; set; } //  khô or lạnh
        public int? ContainerSize { get; set; } // 20 or 40
        public int? DeliveryType { get; set; } // 1 = N, 2 = X
        public string? PickUpLocation { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? ConReturnLocation { get; set; }
        public int? Price { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public decimal? Distance { get; set; }
        public string? ContainerNumber { get; set; }
        public TimeOnly? CompletionTime { get; set; }
        public string? OrderPlace { get; set; }
    }
}
