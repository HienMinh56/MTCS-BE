using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Request
{
    public class CreatePriceTableRequest
    {
        public double? MinKm { get; set; }

        public double? MaxKm { get; set; }

        public int ContainerSize { get; set; }

        public int ContainerType { get; set; }

        public decimal? MinPricePerKm { get; set; }

        public decimal? MaxPricePerKm { get; set; }
        public int DeliveryType { get; set; }

    }
}
