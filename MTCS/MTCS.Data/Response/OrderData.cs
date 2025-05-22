//using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
//using MTCS.Data.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MTCS.Data.Response
//{
//    public class OrderData
//    {
//        public string OrderId { get; set; }

//        public string TrackingCode { get; set; }

//        public string CustomerId { get; set; }

//        public string CompanyName { get; set; }

//        public decimal? Temperature { get; set; }

//        public decimal? Weight { get; set; }

//        public DateOnly? PickUpDate { get; set; }

//        public DateOnly? DeliveryDate { get; set; }

//        public string Status { get; set; }

//        public string Note { get; set; }

//        public DateTime? CreatedDate { get; set; }

//        public string CreatedBy { get; set; }

//        public DateTime? ModifiedDate { get; set; }

//        public string ModifiedBy { get; set; }

//        public int? ContainerType { get; set; }

//        public string PickUpLocation { get; set; }

//        public string DeliveryLocation { get; set; }

//        public string ConReturnLocation { get; set; }

//        public int? DeliveryType { get; set; }

//        public int? Price { get; set; }

//        public string ContainerNumber { get; set; }

//        public string ContactPerson { get; set; }

//        public string ContactPhone { get; set; }

//        public string OrderPlacer { get; set; }

//        public decimal? Distance { get; set; }

//        public int? ContainerSize { get; set; }

//        public int? IsPay { get; set; }

//        public TimeOnly? CompletionTime { get; set; }

//        public virtual Customer Customer { get; set; }

//        public virtual ICollection<OrderFile> OrderFiles { get; set; } = new List<OrderFile>();

//        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
//    }
//}
