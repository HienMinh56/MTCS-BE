using Microsoft.AspNetCore.Http;
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

        public string Note { get; set; }

        public string ContactPerson { get; set; }

        public string ContactPhone { get; set; }

        public string OrderPlacer { get; set; }


        public int? TotalAmount { get; set; }

    }

    public class OrderDetailRequest
    {
        public string OrderId { get; set; }
        public string ContainerNumber { get; set; }
        public int ContainerType { get; set; }
        public int ContainerSize { get; set; }
        public decimal Weight { get; set; }
        public decimal? Temperature { get; set; }
        public string PickUpLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public string ConReturnLocation { get; set; }
        public TimeOnly CompletionTime { get; set; }
        public decimal? Distance { get; set; }
        public DateOnly PickUpDate { get; set; }
        public DateOnly DeliveryDate { get; set; }
    }
}
