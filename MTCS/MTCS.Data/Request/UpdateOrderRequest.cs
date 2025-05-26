using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class UpdateOrderRequest
    {
        public string? Note { get; set; }
        public int? TotalAmount { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? OrderPlacer { get; set; }
        public int? IsPay { get; set; }
    }

    public class UpdateOrderDetailRequest
    {
        public string ContainerNumber { get; set; }

        public int? ContainerType { get; set; }

        public int? ContainerSize { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Temperature { get; set; }

        public string PickUpLocation { get; set; }

        public string DeliveryLocation { get; set; }

        public string ConReturnLocation { get; set; }

        public TimeOnly? CompletionTime { get; set; }

        public decimal? Distance { get; set; }

        public DateOnly? PickUpDate { get; set; }

        public DateOnly? DeliveryDate { get; set; }
        public List<string> Descriptions { get; set; } = new();
        public List<string> Notes { get; set; } = new();
        public List<string> FileIdsToRemove { get; set; } = new();
        public IFormFileCollection? AddedFiles { get; set; } = null;
    }
}
