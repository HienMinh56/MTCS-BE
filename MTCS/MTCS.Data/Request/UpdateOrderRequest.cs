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
        public int? Price { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContainerNumber { get; set; }
        public string? ContactPhone { get; set; }
        public string? OrderPlacer { get; set; }
        public int? IsPay { get; set; }
        public decimal? Temperature { get; set; }
        public List<string> Descriptions { get; set; } = new();
        public List<string> Notes { get; set; } = new();
        public List<string> FileIdsToRemove { get; set; } = new();
        public IFormFileCollection? AddedFiles { get; set; } = null;
    }
}
