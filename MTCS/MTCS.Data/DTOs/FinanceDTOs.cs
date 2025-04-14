namespace MTCS.Data.DTOs
{
    public class RevenueAnalyticsDTO
    {
        public decimal TotalRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageRevenuePerOrder { get; set; }
        public string Period { get; set; }
    }

    public class CustomerRevenueDTO
    {
        public string CustomerId { get; set; }
        public string CompanyName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageRevenuePerOrder { get; set; }
    }

    public class TripFinancialDTO
    {
        public string TripId { get; set; }
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public decimal Revenue { get; set; }
        public decimal FuelCost { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
    }

    public class ProfitAnalyticsDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
        public string Period { get; set; }
    }
}
