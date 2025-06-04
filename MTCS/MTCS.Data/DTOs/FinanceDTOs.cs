namespace MTCS.Data.DTOs
{
    public class RevenueAnalyticsDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncidentCosts { get; set; }
        public decimal NetRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageRevenuePerOrder { get; set; }
        public decimal AverageNetRevenuePerOrder { get; set; }
        public string Period { get; set; }

        // Paid orders data
        public decimal PaidRevenue { get; set; }
        public decimal PaidExpenses { get; set; }
        public decimal PaidIncidentCosts { get; set; }
        public decimal PaidNetRevenue { get; set; }

        // Unpaid orders data
        public decimal UnpaidRevenue { get; set; }
        public decimal UnpaidExpenses { get; set; }
        public decimal UnpaidIncidentCosts { get; set; }
        public decimal UnpaidNetRevenue { get; set; }

        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }

        // Detailed breakdowns
        public Dictionary<string, decimal> ExpenseBreakdown { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<int, decimal> IncidentCostBreakdown { get; set; } = new Dictionary<int, decimal>();

        // Paid/Unpaid breakdowns
        public Dictionary<string, decimal> PaidExpenseBreakdown { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<int, decimal> PaidIncidentCostBreakdown { get; set; } = new Dictionary<int, decimal>();
        public Dictionary<string, decimal> UnpaidExpenseBreakdown { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<int, decimal> UnpaidIncidentCostBreakdown { get; set; } = new Dictionary<int, decimal>();

        public List<OrderSummaryDTO> PaidOrdersList { get; set; } = new List<OrderSummaryDTO>();
        public List<OrderSummaryDTO> UnpaidOrdersList { get; set; } = new List<OrderSummaryDTO>();
        public List<PeriodicRevenueItemDTO> PeriodicData { get; set; } = new List<PeriodicRevenueItemDTO>();
    }

    public class OrderSummaryDTO
    {
        public string OrderId { get; set; }
        public string TrackingCode { get; set; }
        public string CustomerId { get; set; }
        public string CompanyName { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public int? Price { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class CustomerRevenueDTO
    {
        public string CustomerId { get; set; }
        public string CompanyName { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncidentCosts { get; set; }
        public decimal NetRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageRevenuePerOrder { get; set; }
        public decimal PaidRevenue { get; set; }
        public decimal UnpaidRevenue { get; set; }
        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
    }
    public class TripFinancialDTO
    {
        public string TripId { get; set; }
        public string OrderId { get; set; }
        public string TrackingCode { get; set; }
        public string CustomerName { get; set; }
        public decimal Revenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal IncidentCost { get; set; }
        public string Status { get; set; }
        public Dictionary<string, decimal> ExpenseBreakdown { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal ProfitMarginPercentage { get; set; }
    }

    public class TripPerformanceDTO
    {
        public string Period { get; set; }

        public int TotalTrips { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal AverageDistance { get; set; }

        public decimal TotalFuelCost { get; set; }
        public decimal AverageFuelCost { get; set; }
        public decimal FuelCostPerDistance { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal AverageExpensesPerTrip { get; set; }
        public decimal ExpensesPerDistance { get; set; }

        // Performance metrics
        public decimal IncidentRate { get; set; }
        public decimal OnTimeDeliveryRate { get; set; }

        // Driver metrics - sorted lists
        public List<DriverTripDTO> DriversWithMostTrips { get; set; } = new List<DriverTripDTO>();
        public List<DriverHoursDTO> DriversWithMostHours { get; set; } = new List<DriverHoursDTO>();
    }

    public class DriverTripDTO
    {
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public int CompletedTrips { get; set; }
        public decimal TotalDistance { get; set; }
        public int OnTimeDeliveries { get; set; }
        public decimal OnTimePercentage { get; set; }
        public int IncidentsCount { get; set; }
        public decimal IncidentRate { get; set; }
    }
    public class DriverHoursDTO
    {
        public string DriverId { get; set; }
        public string DriverName { get; set; }
        public decimal TotalHours { get; set; }
        public int DaysWorked { get; set; }
        public decimal DailyAverageHours { get; set; }
    }

    public class PeriodicRevenueItemDTO
    {
        public string PeriodLabel { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncidentCosts { get; set; }
        public decimal NetRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageRevenuePerOrder { get; set; }
        public decimal AverageNetRevenuePerOrder { get; set; }
        public decimal PaidRevenue { get; set; }
        public decimal UnpaidRevenue { get; set; }
        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
        public Dictionary<string, decimal> ExpenseBreakdown { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<int, decimal> IncidentCostBreakdown { get; set; } = new Dictionary<int, decimal>();
    }

    public class ExpenseBreakdownDTO
    {
        public string ExpenseType { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

}
