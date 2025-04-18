using MTCS.Data.Models;

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
        public int Status { get; set; }
        public int Version { get; set; }
    }

    public class PriceTableResponse
    {
        public string PriceId { get; set; }
        public double? MinKm { get; set; }
        public double? MaxKm { get; set; }
        public decimal? MinPricePerKm { get; set; }
        public decimal? MaxPricePerKm { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public class CalculatedPriceResponse
    {
        public decimal? BasePrice { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? HighestPrice { get; set; }
    }

    public class PriceTablesHistoryDTO
    {
        public List<PriceTable> PriceTables { get; set; } = new List<PriceTable>();
        public List<int> AvailableVersions { get; set; } = new List<int>();
        public int CurrentVersion { get; set; }
        public int ActiveVersion { get; set; }
        public int TotalCount { get; set; }
    }

    public class PriceChangeGroup
    {
        public int ContainerSize { get; set; }
        public int ContainerType { get; set; }
        public int DeliveryType { get; set; }
        public double MinKm { get; set; }
        public double MaxKm { get; set; }
        public List<PriceTable> Changes { get; set; } = new();
    }
}
