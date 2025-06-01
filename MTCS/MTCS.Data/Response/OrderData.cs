using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Response
{
    public class OrderData
    {
        public string OrderId { get; set; }

        public string TrackingCode { get; set; }

        public string CustomerId { get; set; }

        public string CompanyName { get; set; }

        public string Status { get; set; }

        public string Note { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public string ModifiedBy { get; set; }

        public string ContactPerson { get; set; }

        public string ContactPhone { get; set; }

        public string OrderPlacer { get; set; }

        public int? IsPay { get; set; }

        public int? TotalAmount { get; set; }
        public int? Quantity { get; set; }
    }
}
