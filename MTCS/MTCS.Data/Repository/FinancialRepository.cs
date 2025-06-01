using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class FinancialRepository : GenericRepository<Order>
    {
        public FinancialRepository() : base() { }
        public FinancialRepository(MTCSContext context) : base(context) { }

        private async Task<(decimal TotalAmount, Dictionary<string, decimal> Breakdown, decimal PaidAmount, decimal UnpaidAmount, Dictionary<string, decimal> PaidBreakdown, Dictionary<string, decimal> UnpaidBreakdown)> CalculateExpensesAsync(List<string> tripIds)
        {
            var expenses = await _context.ExpenseReports
                .AsNoTracking()
                .Include(e => e.ReportType)
                .Where(e => tripIds.Contains(e.TripId))
                .ToListAsync();

            decimal totalAmount = expenses.Sum(e => e.Cost ?? 0);

            var paidExpenses = expenses.Where(e => e.IsPay == 1).ToList();
            var unpaidExpenses = expenses.Where(e => e.IsPay != 1).ToList();

            decimal paidAmount = paidExpenses.Sum(e => e.Cost ?? 0);
            decimal unpaidAmount = unpaidExpenses.Sum(e => e.Cost ?? 0);

            // Build expense breakdown by type
            var breakdown = expenses
                .GroupBy(e => e.ReportType?.ReportType ?? "Unknown")
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                );

            // Build paid expense breakdown
            var paidBreakdown = paidExpenses
                .GroupBy(e => e.ReportType?.ReportType ?? "Unknown")
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                );

            // Build unpaid expense breakdown
            var unpaidBreakdown = unpaidExpenses
                .GroupBy(e => e.ReportType?.ReportType ?? "Unknown")
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                );

            return (totalAmount, breakdown, paidAmount, unpaidAmount, paidBreakdown, unpaidBreakdown);
        }

        private async Task<(decimal TotalAmount, Dictionary<int, decimal> Breakdown, decimal PaidAmount, decimal UnpaidAmount, Dictionary<int, decimal> PaidBreakdown, Dictionary<int, decimal> UnpaidBreakdown)> CalculateIncidentCostsAsync(List<string> tripIds)
        {
            var incidents = await _context.IncidentReports
                .AsNoTracking()
                .Where(i => tripIds.Contains(i.TripId))
                .ToListAsync();

            decimal totalAmount = incidents.Sum(i => i.Price ?? 0);

            var paidIncidents = incidents.Where(i => i.IsPay == 1).ToList();
            var unpaidIncidents = incidents.Where(i => i.IsPay != 1).ToList();

            decimal paidAmount = paidIncidents.Sum(i => i.Price ?? 0);
            decimal unpaidAmount = unpaidIncidents.Sum(i => i.Price ?? 0);

            // Build incident breakdown by type
            var breakdown = incidents
                .GroupBy(i => i.Type ?? 0)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(i => i.Price ?? 0), 2)
                );

            // Build paid incident breakdown
            var paidBreakdown = paidIncidents
                .GroupBy(i => i.Type ?? 0)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(i => i.Price ?? 0), 2)
                );

            // Build unpaid incident breakdown
            var unpaidBreakdown = unpaidIncidents
                .GroupBy(i => i.Type ?? 0)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(i => i.Price ?? 0), 2)
                );

            // Ensure all incident types exist in the dictionaries
            foreach (var dictionary in new[] { breakdown, paidBreakdown, unpaidBreakdown })
            {
                if (!dictionary.ContainsKey(1)) dictionary[1] = 0;
                if (!dictionary.ContainsKey(2)) dictionary[2] = 0;
                if (!dictionary.ContainsKey(3)) dictionary[3] = 0;
            }

            return (totalAmount, breakdown, paidAmount, unpaidAmount, paidBreakdown, unpaidBreakdown);
        }

        public async Task<RevenueAnalyticsDTO> GetRevenueAnalyticsAsync(
    RevenuePeriodType periodType,
    DateTime startDate,
    DateTime? endDate = null)
        {
            DateTime periodEndDate;
            string periodLabel;
            List<PeriodicRevenueItemDTO> periodicData = new List<PeriodicRevenueItemDTO>();

            switch (periodType)
            {
                case RevenuePeriodType.Monthly:
                    startDate = new DateTime(startDate.Year, startDate.Month, 1);
                    periodEndDate = startDate.AddMonths(1);
                    periodLabel = $"{startDate:MMMM yyyy}";

                    // Generate data for each day in the month
                    for (DateTime day = startDate; day < periodEndDate; day = day.AddDays(1))
                    {
                        periodicData.Add(new PeriodicRevenueItemDTO
                        {
                            PeriodLabel = day.ToString("MM/dd/yyyy"),
                            StartDate = day,
                            EndDate = day.AddDays(1)
                        });
                    }
                    break;

                case RevenuePeriodType.Yearly:
                    startDate = new DateTime(startDate.Year, 1, 1);
                    periodEndDate = startDate.AddYears(1);
                    periodLabel = $"{startDate.Year}";

                    // Generate data for each month in the year
                    for (int month = 1; month <= 12; month++)
                    {
                        var monthStart = new DateTime(startDate.Year, month, 1);
                        var monthEnd = monthStart.AddMonths(1);
                        periodicData.Add(new PeriodicRevenueItemDTO
                        {
                            PeriodLabel = monthStart.ToString("MMMM yyyy"),
                            StartDate = monthStart,
                            EndDate = monthEnd
                        });
                    }
                    break;

                case RevenuePeriodType.Custom:
                    if (!endDate.HasValue)
                        throw new ArgumentException("End date must be provided for custom period type");

                    periodEndDate = endDate.Value;
                    periodLabel = $"{startDate:MM/dd/yyyy} - {periodEndDate:MM/dd/yyyy}";
                    break;

                default:
                    throw new ArgumentException("Invalid period type");
            }

            var orders = await _context.Orders.AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Where(o => o.CreatedDate >= startDate &&
                            o.CreatedDate < periodEndDate &&
                            o.Status == "Completed")
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderId).ToList();

            var orderDetailIds = orders
                .SelectMany(o => o.OrderDetails.Select(od => od.OrderDetailId))
                .ToList();

            var trips = await _context.Trips
                .Where(t => orderDetailIds.Contains(t.OrderDetailId))
                .ToListAsync();

            var tripIds = trips.Select(t => t.TripId).ToList();

            var (totalExpenses, expenseBreakdown, paidExpensesAmount, unpaidExpensesAmount, paidExpenseBreakdown, unpaidExpenseBreakdown) = await CalculateExpensesAsync(tripIds);

            var (totalIncidentCosts, incidentBreakdown, paidIncidentsAmount, unpaidIncidentsAmount, paidIncidentBreakdown, unpaidIncidentBreakdown) = await CalculateIncidentCostsAsync(tripIds);

            // Revenue calculations
            decimal totalRevenue = orders.Sum(o => o.TotalAmount ?? 0);
            int orderCount = orders.Count;
            decimal netRevenue = totalRevenue - totalExpenses - totalIncidentCosts;
            decimal averageRevenue = orderCount > 0 ? Math.Round(totalRevenue / orderCount, 2) : 0;
            decimal averageNetRevenue = orderCount > 0 ? Math.Round(netRevenue / orderCount, 2) : 0;

            // Process paid vs unpaid orders
            var paidOrders = orders.Where(o => o.IsPay == 1).ToList();
            var unpaidOrders = orders.Where(o => o.IsPay != 1).ToList();

            decimal paidRevenue = paidOrders.Sum(o => o.TotalAmount ?? 0);
            decimal unpaidRevenue = unpaidOrders.Sum(o => o.TotalAmount ?? 0);
            int paidOrderCount = paidOrders.Count;
            int unpaidOrderCount = unpaidOrders.Count;

            // Get paid and unpaid trip IDs
            var paidOrderDetailIds = paidOrders
                .SelectMany(o => o.OrderDetails.Select(od => od.OrderDetailId))
                .ToList();

            var unpaidOrderDetailIds = unpaidOrders
                .SelectMany(o => o.OrderDetails.Select(od => od.OrderDetailId))
                .ToList();

            decimal netPaidRevenue = paidRevenue - paidExpensesAmount - paidIncidentsAmount;
            decimal netUnpaidRevenue = unpaidRevenue - unpaidExpensesAmount - unpaidIncidentsAmount;

            // Process periodic data
            foreach (var item in periodicData)
            {
                var periodOrders = orders.Where(o =>
                    o.CreatedDate >= item.StartDate &&
                    o.CreatedDate < item.EndDate).ToList();

                var periodPaidOrders = periodOrders.Where(o => o.IsPay == 1).ToList();
                var periodUnpaidOrders = periodOrders.Where(o => o.IsPay != 1).ToList();

                // Get period trip IDs
                var periodOrderDetailIds = periodOrders
                    .SelectMany(o => o.OrderDetails.Select(od => od.OrderDetailId))
                    .ToList();

                var periodTripIds = trips
                    .Where(t => periodOrderDetailIds.Contains(t.OrderDetailId))
                    .Select(t => t.TripId)
                    .ToList();

                // Calculate period expenses
                var (periodExpenses, periodExpenseBreakdown, periodPaidExpenses, periodUnpaidExpenses, _, _) =
    await CalculateExpensesAsync(periodTripIds);

                var (periodIncidentCosts, periodIncidentBreakdown, periodPaidIncidents, periodUnpaidIncidents, _, _) =
                    await CalculateIncidentCostsAsync(periodTripIds);

                item.TotalRevenue = periodOrders.Sum(o => o.TotalAmount ?? 0);
                item.TotalExpenses = periodExpenses;
                item.TotalIncidentCosts = periodIncidentCosts;
                item.NetRevenue = item.TotalRevenue - periodExpenses - periodIncidentCosts;
                item.CompletedOrders = periodOrders.Count;
                item.AverageRevenuePerOrder = item.CompletedOrders > 0
                    ? Math.Round(item.TotalRevenue / item.CompletedOrders, 2)
                    : 0;
                item.AverageNetRevenuePerOrder = item.CompletedOrders > 0
                    ? Math.Round(item.NetRevenue / item.CompletedOrders, 2)
                    : 0;
                item.PaidRevenue = periodPaidOrders.Sum(o => o.TotalAmount ?? 0);
                item.UnpaidRevenue = periodUnpaidOrders.Sum(o => o.TotalAmount ?? 0);
                item.PaidOrders = periodPaidOrders.Count;
                item.UnpaidOrders = periodUnpaidOrders.Count;
                item.ExpenseBreakdown = periodExpenseBreakdown;
                item.IncidentCostBreakdown = periodIncidentBreakdown;
            }

            var paidOrdersList = paidOrders.Select(o => new OrderSummaryDTO
            {
                OrderId = o.OrderId,
                TrackingCode = o.TrackingCode,
                CustomerId = o.CustomerId,
                CompanyName = o.Customer?.CompanyName,
                DeliveryDate = o.OrderDetails.FirstOrDefault()?.DeliveryDate,
                Price = o.TotalAmount,
                Status = o.Status,
                CreatedDate = o.CreatedDate
            }).ToList();

            var unpaidOrdersList = unpaidOrders.Select(o => new OrderSummaryDTO
            {
                OrderId = o.OrderId,
                TrackingCode = o.TrackingCode,
                CustomerId = o.CustomerId,
                CompanyName = o.Customer?.CompanyName,
                DeliveryDate = o.OrderDetails.FirstOrDefault()?.DeliveryDate,
                Price = o.TotalAmount,
                Status = o.Status,
                CreatedDate = o.CreatedDate
            }).ToList();

            return new RevenueAnalyticsDTO
            {
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                TotalIncidentCosts = totalIncidentCosts,
                NetRevenue = netRevenue,
                CompletedOrders = orderCount,
                AverageRevenuePerOrder = averageRevenue,
                AverageNetRevenuePerOrder = averageNetRevenue,
                Period = periodLabel,
                PaidRevenue = paidRevenue,
                PaidExpenses = paidExpensesAmount,
                PaidIncidentCosts = paidIncidentsAmount,
                PaidNetRevenue = netPaidRevenue,
                UnpaidRevenue = unpaidRevenue,
                UnpaidExpenses = unpaidExpensesAmount,
                UnpaidIncidentCosts = unpaidIncidentsAmount,
                UnpaidNetRevenue = netUnpaidRevenue,
                PaidOrders = paidOrderCount,
                UnpaidOrders = unpaidOrderCount,
                ExpenseBreakdown = expenseBreakdown,
                IncidentCostBreakdown = incidentBreakdown,
                PaidExpenseBreakdown = paidExpenseBreakdown,
                PaidIncidentCostBreakdown = paidIncidentBreakdown,
                UnpaidExpenseBreakdown = unpaidExpenseBreakdown,
                UnpaidIncidentCostBreakdown = unpaidIncidentBreakdown,
                PaidOrdersList = paidOrdersList,
                UnpaidOrdersList = unpaidOrdersList,
                PeriodicData = periodicData
            };
        }

        public async Task<PagedList<CustomerRevenueDTO>> GetRevenueByCustomerAsync(
     PaginationParams paginationParams,
     DateTime? startDate = null,
     DateTime? endDate = null)
        {
            var ordersQuery = _context.Orders.AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Where(o => o.Status == "Completed" || o.IsPay == 1);

            if (startDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate >= startDate);

            if (endDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedDate < endDate);

            var orders = await ordersQuery.ToListAsync();

            var orderDetailIds = orders
                .SelectMany(o => o.OrderDetails.Select(od => od.OrderDetailId))
                .ToList();

            var trips = await _context.Trips.AsNoTracking()
                .Where(t => orderDetailIds.Contains(t.OrderDetailId))
                .ToListAsync();

            var tripIds = trips.Select(t => t.TripId).ToList();

            // Calculate expenses and incident costs for all trips
            var (totalExpenses, expenseBreakdown, paidExpensesAmount, unpaidExpensesAmount, paidExpenseBreakdown, unpaidExpenseBreakdown) =
     await CalculateExpensesAsync(tripIds);

            var (totalIncidentCosts, incidentBreakdown, paidIncidentsAmount, unpaidIncidentsAmount, paidIncidentBreakdown, unpaidIncidentBreakdown) =
                await CalculateIncidentCostsAsync(tripIds);

            // Create lookup for trip expenses and incidents
            var tripExpenses = await _context.ExpenseReports.AsNoTracking()
                .Include(e => e.ReportType)
                .Where(e => tripIds.Contains(e.TripId))
                .GroupBy(e => e.TripId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Sum(e => e.Cost ?? 0)
                );

            var tripIncidents = await _context.IncidentReports.AsNoTracking()
                .Where(i => tripIds.Contains(i.TripId))
                .GroupBy(i => i.TripId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Sum(i => i.Price ?? 0)
                );

            // Group orders by customer and calculate metrics
            var customerGroups = orders
                .GroupBy(o => new { o.CustomerId, o.Customer?.CompanyName })
                .Select(g =>
                {
                    // Get order detail IDs for this customer
                    var customerOrderDetailIds = g.SelectMany(o => o.OrderDetails.Select(od => od.OrderDetailId)).ToList();

                    // Get trips for this customer
                    var customerTrips = trips.Where(t => customerOrderDetailIds.Contains(t.OrderDetailId)).ToList();
                    var customerTripIds = customerTrips.Select(t => t.TripId).ToList();

                    // Calculate customer expenses
                    decimal customerExpenses = customerTripIds
                        .Where(tripId => tripExpenses.ContainsKey(tripId))
                        .Sum(tripId => tripExpenses[tripId]);

                    // Calculate customer incident costs
                    decimal customerIncidentCosts = customerTripIds
                        .Where(tripId => tripIncidents.ContainsKey(tripId))
                        .Sum(tripId => tripIncidents[tripId]);

                    // Calculate revenue
                    decimal totalRevenue = g.Sum(o => o.TotalAmount ?? 0);

                    return new CustomerRevenueDTO
                    {
                        CustomerId = g.Key.CustomerId,
                        CompanyName = g.Key.CompanyName,
                        TotalRevenue = totalRevenue,
                        TotalExpenses = customerExpenses,
                        TotalIncidentCosts = customerIncidentCosts,
                        NetRevenue = totalRevenue - customerExpenses - customerIncidentCosts,
                        CompletedOrders = g.Count(),
                        AverageRevenuePerOrder = g.Count() > 0 ? totalRevenue / g.Count() : 0,
                        PaidRevenue = g.Where(o => o.IsPay == 1).Sum(o => o.TotalAmount ?? 0),
                        UnpaidRevenue = g.Where(o => o.Status == "Completed" && o.IsPay != 1).Sum(o => o.TotalAmount ?? 0),
                        PaidOrders = g.Count(o => o.IsPay == 1),
                        UnpaidOrders = g.Count(o => o.Status == "Completed" && o.IsPay != 1)
                    };
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();

            return PagedList<CustomerRevenueDTO>.CreateFromList(
                customerGroups,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<TripFinancialDTO> GetTripFinancialDetailsAsync(string tripId)
        {
            var trip = await _context.Trips
                .AsNoTracking()
                .Include(t => t.OrderDetail)
                .Include(t => t.OrderDetail.Order)
                .Include(t => t.OrderDetail.Order.Customer)
                .Include(t => t.ExpenseReports)
                .ThenInclude(e => e.ReportType)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
                return null;

            var revenue = trip.OrderDetail.Order?.TotalAmount ?? 0;

            // Group expenses by type
            var expenseBreakdown = trip.ExpenseReports
                .Where(e => e.ReportType != null)
                .GroupBy(e => e.ReportType.ReportType)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                );

            var totalExpenses = Math.Round(trip.ExpenseReports.Sum(e => e.Cost ?? 0), 2);
            var profit = Math.Round(revenue - totalExpenses, 2);
            var profitMarginPercentage = revenue > 0 ? Math.Round((profit / revenue) * 100, 2) : 0;

            return new TripFinancialDTO
            {
                TripId = trip.TripId,
                OrderId = trip.OrderDetail.OrderId,
                TrackingCode = trip.OrderDetail.Order.TrackingCode,
                CustomerName = trip.OrderDetail.Order?.Customer?.CompanyName,
                Revenue = revenue,
                TotalExpenses = totalExpenses,
                ExpenseBreakdown = expenseBreakdown,
                ProfitMargin = profit,
                ProfitMarginPercentage = profitMarginPercentage
            };
        }

        public async Task<List<TripFinancialDTO>> GetTripsFinancialDetailsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string customerId = null)
        {
            var query = _context.Trips.AsNoTracking()
                .Include(t => t.OrderDetail)
                    .ThenInclude(od => od.Order)
                        .ThenInclude(o => o.Customer)
                .Include(t => t.ExpenseReports)
                    .ThenInclude(e => e.ReportType)
                    .Include(t => t.IncidentReports)
                    .Where(t => t.MatchTime != null);

            if (startDate.HasValue)
                query = query.Where(t => t.StartTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.EndTime == null || t.EndTime <= endDate);

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(t => t.OrderDetail.Order.CustomerId == customerId);

            var trips = await query.OrderByDescending(t => t.StartTime).ToListAsync();

            return trips.Select(trip =>
            {
                var revenue = trip.OrderDetail?.Order?.TotalAmount ?? 0;

                var expenseBreakdown = trip.ExpenseReports
                    .Where(e => e.ReportType != null)
                    .GroupBy(e => e.ReportType.ReportType)
                    .ToDictionary(
                        g => g.Key,
                        g => Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                    );

                var totalExpenses = Math.Round(trip.ExpenseReports.Sum(e => e.Cost ?? 0), 2);

                decimal incidentCost = 0;
                if (trip.IncidentReports != null && trip.IncidentReports.Any())
                {
                    foreach (var incident in trip.IncidentReports)
                    {
                        decimal incidentPrice = incident.Price ?? 0;
                        incidentCost += incidentPrice;

                        string incidentTypeKey = $"Incident-Type-{incident.Type ?? 0}";

                        if (!expenseBreakdown.ContainsKey(incidentTypeKey))
                        {
                            expenseBreakdown[incidentTypeKey] = 0;
                        }

                        expenseBreakdown[incidentTypeKey] += incidentPrice;
                    }
                }

                var profit = revenue - (totalExpenses + incidentCost);
                var profitMarginPercentage = revenue > 0 ? Math.Round((profit / revenue) * 100, 2) : 0;

                return new TripFinancialDTO
                {
                    TripId = trip.TripId,
                    OrderId = trip.OrderDetail?.OrderId,
                    TrackingCode = trip.OrderDetail?.Order?.TrackingCode,
                    CustomerName = trip.OrderDetail?.Order?.Customer?.CompanyName,
                    Status = trip.Status,
                    Revenue = revenue,
                    IncidentCost = Math.Round(incidentCost, 2),
                    TotalExpenses = totalExpenses,
                    ExpenseBreakdown = expenseBreakdown,
                    ProfitMargin = profit,
                    ProfitMarginPercentage = profitMarginPercentage
                };
            }).ToList();
        }

        public async Task<TripPerformanceDTO> GetTripPerformanceAsync(DateTime startDate, DateTime endDate)
        {
            var trips = await _context.Trips
                .AsNoTracking()
                .Include(t => t.OrderDetail)
                .Include(t => t.OrderDetail.Order)
                .Include(t => t.Driver)
                .Include(t => t.ExpenseReports)
                    .ThenInclude(e => e.ReportType)
                .Include(t => t.IncidentReports)
                .Where(t => t.EndTime >= startDate &&
                           t.EndTime <= endDate &&
                           t.Status == "completed")
                .ToListAsync();

            if (!trips.Any())
            {
                return new TripPerformanceDTO
                {
                    Period = $"{startDate:MM/dd/yyyy} - {endDate:MM/dd/yyyy}",
                    TotalTrips = 0,
                    TotalDistance = 0,
                    AverageDistance = 0,
                    TotalFuelCost = 0,
                    AverageFuelCost = 0,
                    FuelCostPerDistance = 0,
                    TotalExpenses = 0,
                    AverageExpensesPerTrip = 0,
                    ExpensesPerDistance = 0,
                    IncidentRate = 0,
                    OnTimeDeliveryRate = 0,
                    DriversWithMostTrips = new List<DriverTripDTO>(),
                    DriversWithMostHours = new List<DriverHoursDTO>()
                };
            }

            int totalTrips = trips.Count;
            decimal totalDistance = trips.Sum(t => t.OrderDetail?.Distance ?? 0);

            // Get expense types from database
            var expenseTypesData = await _context.ExpenseReportTypes
                .Where(t => t.IsActive == 1)
                .ToListAsync();

            // Total expenses across all categories
            decimal totalExpenses = Math.Round(trips.Sum(t => t.ExpenseReports.Sum(e => e.Cost ?? 0)), 2);

            // Group expenses by type
            var expensesByType = trips
                .SelectMany(t => t.ExpenseReports)
                .Where(e => e.ReportType != null)
                .GroupBy(e => e.ReportType.ReportType)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                );

            // For backward compatibility, try to get fuel cost if it exists
            decimal totalFuelCost = 0;
            expensesByType.TryGetValue("fuel_report", out totalFuelCost);

            decimal averageDistance = totalTrips > 0 ? Math.Round(totalDistance / totalTrips, 2) : 0;
            decimal averageFuelCost = totalTrips > 0 ? Math.Round(totalFuelCost / totalTrips, 2) : 0;
            decimal fuelCostPerDistance = totalDistance > 0 ? Math.Round(totalFuelCost / totalDistance, 2) : 0;

            // Additional expense metrics
            decimal averageExpensesPerTrip = totalTrips > 0 ? Math.Round(totalExpenses / totalTrips, 2) : 0;
            decimal expensesPerDistance = totalDistance > 0 ? Math.Round(totalExpenses / totalDistance, 2) : 0;

            int tripsWithIncidents = trips.Count(t => t.IncidentReports.Any());
            decimal incidentRate = totalTrips > 0 ? Math.Round((decimal)tripsWithIncidents / totalTrips * 100, 2) : 0;

            int onTimeDeliveries = trips.Count(t =>
                t.EndTime.HasValue &&
                t.OrderDetail?.DeliveryDate.HasValue == true &&
                t.EndTime.Value.Date <= t.OrderDetail.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue));

            decimal onTimeRate = totalTrips > 0 ? Math.Round((decimal)onTimeDeliveries / totalTrips * 100, 2) : 0;

            var driversWithMostTrips = trips
                .Where(t => t.Driver != null)
                .GroupBy(t => new { t.DriverId, t.Driver.FullName })
                .Select(g => new DriverTripDTO
                {
                    DriverId = g.Key.DriverId,
                    DriverName = g.Key.FullName ?? "Unknown",
                    CompletedTrips = g.Count(),
                    TotalDistance = g.Sum(t => t.OrderDetail?.Distance ?? 0),
                    OnTimeDeliveries = g.Count(t =>
                        t.EndTime.HasValue &&
                        t.OrderDetail?.DeliveryDate.HasValue == true &&
                        t.EndTime.Value.Date <= t.OrderDetail.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue)),
                    IncidentsCount = g.Count(t => t.IncidentReports.Any())
                })
                .OrderByDescending(d => d.CompletedTrips)
                .ToList();

            foreach (var driver in driversWithMostTrips)
            {
                driver.OnTimePercentage = driver.CompletedTrips > 0
                    ? Math.Round((decimal)driver.OnTimeDeliveries / driver.CompletedTrips * 100, 2)
                    : 0;

                driver.IncidentRate = driver.CompletedTrips > 0
                    ? Math.Round((decimal)driver.IncidentsCount / driver.CompletedTrips * 100, 2)
                    : 0;
            }

            var driverWorkingTimesData = await _context.DriverDailyWorkingTimes
                .Include(d => d.Driver)
                .Where(d => d.WorkDate.HasValue &&
                           DateOnly.FromDateTime(startDate) <= d.WorkDate.Value &&
                           d.WorkDate.Value <= DateOnly.FromDateTime(endDate))
                .ToListAsync();

            var driverWorkingHours = driverWorkingTimesData
                .GroupBy(d => new { d.DriverId, DriverName = d.Driver?.FullName })
                .Select(g =>
                {
                    // Calculate total minutes first
                    int totalMinutes = g.Sum(d => d.TotalTime ?? 0);
                    // Then convert to hours
                    decimal totalHours = Math.Round((decimal)totalMinutes / 60, 2);

                    return new DriverHoursDTO
                    {
                        DriverId = g.Key.DriverId,
                        DriverName = g.Key.DriverName ?? "Unknown",
                        TotalHours = totalHours, // Explicitly set total hours
                        DaysWorked = g.Count(),
                        DailyAverageHours = g.Count() > 0
                            ? Math.Round(totalHours / g.Count(), 2)
                            : 0
                    };
                })
                .OrderByDescending(d => d.TotalHours)
                .ToList();

            return new TripPerformanceDTO
            {
                Period = $"{startDate:MM/dd/yyyy} - {endDate:MM/dd/yyyy}",
                TotalTrips = totalTrips,
                TotalDistance = totalDistance,
                AverageDistance = averageDistance,
                TotalFuelCost = totalFuelCost,
                AverageFuelCost = averageFuelCost,
                FuelCostPerDistance = fuelCostPerDistance,
                TotalExpenses = totalExpenses,
                AverageExpensesPerTrip = averageExpensesPerTrip,
                ExpensesPerDistance = expensesPerDistance,
                IncidentRate = incidentRate,
                OnTimeDeliveryRate = onTimeRate,
                DriversWithMostTrips = driversWithMostTrips,
                DriversWithMostHours = driverWorkingHours
            };
        }

        public async Task<decimal> GetAverageFuelCostPerDistanceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // First, find fuel report type from database
            var fuelReportType = await _context.ExpenseReportTypes
                .Where(t => t.ReportType.Contains("fuel") && t.IsActive == 1)
                .FirstOrDefaultAsync();

            if (fuelReportType == null)
                return 0; // No fuel report type found

            var query = _context.Trips
                .Include(t => t.OrderDetail)
                .Include(t => t.ExpenseReports)
                    .ThenInclude(e => e.ReportType)
                .Where(t => t.Status == "completed" && t.OrderDetail.Distance > 0);

            if (startDate.HasValue)
                query = query.Where(t => t.EndTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.EndTime <= endDate);

            var trips = await query.ToListAsync();

            if (!trips.Any())
                return 0;

            decimal totalFuelCost = Math.Round(trips.Sum(t => t.ExpenseReports
                .Where(e => e.ReportType != null && e.ReportType.ReportTypeId == fuelReportType.ReportTypeId)
                .Sum(f => f.Cost ?? 0)), 2);

            decimal totalDistance = Math.Round(trips.Sum(t => t.OrderDetail.Distance ?? 0), 2);

            return totalDistance > 0 ? Math.Round(totalFuelCost / totalDistance, 2) : 0;
        }

        public async Task<Dictionary<string, decimal>> GetExpenseBreakdownByTypeAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.Trips
                .Include(t => t.ExpenseReports)
                    .ThenInclude(e => e.ReportType)
                .Where(t => t.Status == "completed");

            if (startDate.HasValue)
                query = query.Where(t => t.EndTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.EndTime <= endDate);

            var trips = await query.ToListAsync();

            // Initialize dictionary with all expense types from the database
            var allExpenseTypes = await _context.ExpenseReportTypes
                .Where(t => t.IsActive == 1)
                .Select(t => t.ReportType)
                .ToListAsync();

            var expenseBreakdown = allExpenseTypes.ToDictionary(
                type => type,
                type => 0m
            );

            if (!trips.Any())
                return expenseBreakdown;

            // Group all expenses by type and calculate totals
            var expenses = trips.SelectMany(t => t.ExpenseReports)
                .Where(e => e.ReportType != null)
                .GroupBy(e => e.ReportType.ReportType)
                .Select(g => new
                {
                    ExpenseType = g.Key,
                    Total = Math.Round(g.Sum(e => e.Cost ?? 0), 2)
                })
                .ToList();

            foreach (var expense in expenses)
            {
                if (expenseBreakdown.ContainsKey(expense.ExpenseType))
                    expenseBreakdown[expense.ExpenseType] = expense.Total;
                else
                    expenseBreakdown.Add(expense.ExpenseType, expense.Total);
            }

            return expenseBreakdown;
        }
    }
}