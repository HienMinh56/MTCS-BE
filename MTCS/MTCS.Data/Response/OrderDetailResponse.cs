using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Response
{
    public class OrderDetailResponse
    {
        public string OrderDetailId { get; set; }
        public string OrderId { get; set; }
        public string ContainerNumber { get; set; }
        public int ContainerType { get; set; }
        public int ContainerSize { get; set; }
        public double Weight { get; set; }
        public DateOnly PickUpDate { get; set; }
        public DateOnly DeliveryDate { get; set; }
        public string PickUpLocation { get; set; }

        public string DeliveryLocation { get; set; }

        public string ConReturnLocation { get; set; }
        public TimeOnly? CompletionTime { get; set; }

        public decimal? Distance { get; set; }

        public string Status { get; set; }

        public List<OrderDetailFileData> Files { get; set; }
    }

    public class OrderDetailFileData
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public string Description { get; set; }
        public string Note { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadBy { get; set; }
    }
}
