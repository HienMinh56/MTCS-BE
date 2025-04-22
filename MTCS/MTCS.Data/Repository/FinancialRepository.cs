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

        public async Task<RevenueAnalyticsDTO> GetRevenueAnalyticsAsync(
     RevenuePeriodType periodType,
     DateTime startDate,
     DateTime? endDate = null)
        {
            DateTime periodEndDate;
            string periodLabel;

            switch (periodType)
            {
                case RevenuePeriodType.Weekly:
                    periodEndDate = startDate.AddDays(7);
                    periodLabel = $"{startDate:MM/dd/yyyy} - {periodEndDate.AddDays(-1):MM/dd/yyyy}";
                    break;

                case RevenuePeriodType.Monthly:
                    startDate = new DateTime(startDate.Year, startDate.Month, 1);
                    periodEndDate = startDate.AddMonths(1);
                    periodLabel = $"{startDate:MMMM yyyy}";
                    break;

                case RevenuePeriodType.Yearly:
                    startDate = new DateTime(startDate.Year, 1, 1);
                    periodEndDate = startDate.AddYears(1);
                    periodLabel = $"{startDate.Year}";
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

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.CreatedDate >= startDate &&
                           o.CreatedDate < periodEndDate &&
                           o.Status == "Completed")
                .ToListAsync();

            decimal totalRevenue = orders.Sum(o => o.Price ?? 0);
            int orderCount = orders.Count;
            decimal averageRevenue = orderCount > 0 ? Math.Round(totalRevenue / orderCount, 2) : 0;

            var paidOrders = orders.Where(o => o.IsPay == 1).ToList();
            var unpaidOrders = orders.Where(o => o.IsPay != 1).ToList();

            decimal paidRevenue = paidOrders.Sum(o => o.Price ?? 0);
            decimal unpaidRevenue = unpaidOrders.Sum(o => o.Price ?? 0);
            int paidOrderCount = paidOrders.Count;
            int unpaidOrderCount = unpaidOrders.Count;

            var paidOrdersList = paidOrders.Select(o => new OrderSummaryDTO
            {
                OrderId = o.OrderId,
                TrackingCode = o.TrackingCode,
                CustomerId = o.CustomerId,
                CompanyName = o.Customer?.CompanyName,
                DeliveryDate = o.DeliveryDate,
                Price = o.Price,
                Status = o.Status,
                CreatedDate = o.CreatedDate
            }).ToList();

            var unpaidOrdersList = unpaidOrders.Select(o => new OrderSummaryDTO
            {
                OrderId = o.OrderId,
                TrackingCode = o.TrackingCode,
                CustomerId = o.CustomerId,
                CompanyName = o.Customer?.CompanyName,
                DeliveryDate = o.DeliveryDate,
                Price = o.Price,
                Status = o.Status,
                CreatedDate = o.CreatedDate
            }).ToList();

            return new RevenueAnalyticsDTO
            {
                TotalRevenue = totalRevenue,
                CompletedOrders = orderCount,
                AverageRevenuePerOrder = averageRevenue,
                Period = periodLabel,
                PaidRevenue = paidRevenue,
                UnpaidRevenue = unpaidRevenue,
                PaidOrders = paidOrderCount,
                UnpaidOrders = unpaidOrderCount,
                PaidOrdersList = paidOrdersList,
                UnpaidOrdersList = unpaidOrdersList
            };
        }

        public async Task<PagedList<CustomerRevenueDTO>> GetRevenueByCustomerAsync(
            PaginationParams paginationParams,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == "Completed" || o.IsPay == 1);

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(o => o.CreatedDate < endDate);

            var customerGroupsQuery = query
                .GroupBy(o => new { o.CustomerId, o.Customer.CompanyName })
                .Select(g => new CustomerRevenueDTO
                {
                    CustomerId = g.Key.CustomerId,
                    CompanyName = g.Key.CompanyName,
                    TotalRevenue = g.Sum(o => o.Price ?? 0),
                    CompletedOrders = g.Count(),
                    AverageRevenuePerOrder = g.Sum(o => o.Price ?? 0) / (g.Count() > 0 ? g.Count() : 1),
                    PaidRevenue = g.Where(o => o.IsPay == 1).Sum(o => o.Price ?? 0),
                    UnpaidRevenue = g.Where(o => o.Status == "Completed" && o.IsPay != 1).Sum(o => o.Price ?? 0),
                    PaidOrders = g.Count(o => o.IsPay == 1),
                    UnpaidOrders = g.Count(o => o.Status == "Completed" && o.IsPay != 1)
                })
                .OrderByDescending(r => r.TotalRevenue);

            return await PagedList<CustomerRevenueDTO>.CreateAsync(
                customerGroupsQuery,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<TripFinancialDTO> GetTripFinancialDetailsAsync(string tripId)
        {
            var trip = await _context.Trips
                .Include(t => t.Order)
                .Include(t => t.Order.Customer)
                .Include(t => t.FuelReports)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
                return null;

            var revenue = trip.Order?.Price ?? 0;
            var fuelCost = Math.Round(trip.FuelReports.Sum(f => f.FuelCost ?? 0), 2);
            var profit = Math.Round(revenue - fuelCost, 2);
            var profitMarginPercentage = revenue > 0 ? Math.Round((profit / revenue) * 100, 2) : 0;


            return new TripFinancialDTO
            {
                TripId = trip.TripId,
                OrderId = trip.OrderId,
                TrackingCode = trip.Order.TrackingCode,
                CustomerName = trip.Order?.Customer?.CompanyName,
                Revenue = revenue,
                FuelCost = fuelCost,
                ProfitMargin = profit,
                ProfitMarginPercentage = profitMarginPercentage
            };
        }

        public async Task<List<TripFinancialDTO>> GetTripsFinancialDetailsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string customerId = null)
        {
            var query = _context.Trips
                .Include(t => t.Order)
                .Include(t => t.Order.Customer)
                .Include(t => t.FuelReports)
                .Where(t => t.Status == "completed");

            if (startDate.HasValue)
                query = query.Where(t => t.EndTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.EndTime <= endDate);

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(t => t.Order.CustomerId == customerId);

            var trips = await query.ToListAsync();

            return trips.Select(trip =>
            {
                var revenue = trip.Order?.Price ?? 0;
                var fuelCost = Math.Round(trip.FuelReports.Sum(f => f.FuelCost ?? 0), 2);
                var profit = Math.Round(revenue - fuelCost, 2);
                var profitMarginPercentage = revenue > 0 ? Math.Round((profit / revenue) * 100, 2) : 0;


                return new TripFinancialDTO
                {
                    TripId = trip.TripId,
                    OrderId = trip.OrderId,
                    TrackingCode = trip.Order.TrackingCode,
                    CustomerName = trip.Order?.Customer?.CompanyName,
                    Revenue = revenue,
                    FuelCost = fuelCost,
                    ProfitMargin = profit,
                    ProfitMarginPercentage = profitMarginPercentage
                };
            }).ToList();
        }

        public async Task<TripPerformanceDTO> GetTripPerformanceAsync(DateTime startDate, DateTime endDate)
        {
            var trips = await _context.Trips
                .Include(t => t.Order)
                .Include(t => t.Driver)
                .Include(t => t.FuelReports)
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
                    IncidentRate = 0,
                    OnTimeDeliveryRate = 0,
                    DriversWithMostTrips = new List<DriverTripDTO>(),
                    DriversWithMostHours = new List<DriverHoursDTO>()
                };
            }

            int totalTrips = trips.Count;
            decimal totalDistance = trips.Sum(t => t.Order?.Distance ?? 0);
            decimal totalFuelCost = Math.Round(trips.Sum(t => t.FuelReports.Sum(f => f.FuelCost ?? 0)), 2);

            decimal averageDistance = totalTrips > 0 ? Math.Round(totalDistance / totalTrips, 2) : 0;
            decimal averageFuelCost = totalTrips > 0 ? Math.Round(totalFuelCost / totalTrips, 2) : 0;
            decimal fuelCostPerDistance = totalDistance > 0 ? Math.Round(totalFuelCost / totalDistance, 2) : 0;

            int tripsWithIncidents = trips.Count(t => t.IncidentReports.Any());
            decimal incidentRate = totalTrips > 0 ? Math.Round((decimal)tripsWithIncidents / totalTrips * 100, 2) : 0;

            int onTimeDeliveries = trips.Count(t =>
                t.EndTime.HasValue &&
                t.Order?.DeliveryDate.HasValue == true &&
                t.EndTime.Value.Date <= t.Order.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue));

            decimal onTimeRate = totalTrips > 0 ? Math.Round((decimal)onTimeDeliveries / totalTrips * 100, 2) : 0;

            var driversWithMostTrips = trips
                .Where(t => t.Driver != null)
                .GroupBy(t => new { t.DriverId, t.Driver.FullName })
                .Select(g => new DriverTripDTO
                {
                    DriverId = g.Key.DriverId,
                    DriverName = g.Key.FullName ?? "Unknown",
                    CompletedTrips = g.Count(),
                    TotalDistance = g.Sum(t => t.Order?.Distance ?? 0),
                    OnTimeDeliveries = g.Count(t =>
                        t.EndTime.HasValue &&
                        t.Order?.DeliveryDate.HasValue == true &&
                        t.EndTime.Value.Date <= t.Order.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue)),
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
                IncidentRate = incidentRate,
                OnTimeDeliveryRate = onTimeRate,
                DriversWithMostTrips = driversWithMostTrips,
                DriversWithMostHours = driverWorkingHours
            };
        }

        public async Task<decimal> GetAverageFuelCostPerDistanceAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Trips
                .Include(t => t.Order)
                .Include(t => t.FuelReports)
                .Where(t => t.Status == "completed" && t.Order.Distance > 0);

            if (startDate.HasValue)
                query = query.Where(t => t.EndTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.EndTime <= endDate);

            var trips = await query.ToListAsync();

            if (!trips.Any())
                return 0;

            decimal totalFuelCost = Math.Round(trips.Sum(t => t.FuelReports.Sum(f => f.FuelCost ?? 0)), 2);
            decimal totalDistance = Math.Round(trips.Sum(t => t.Order.Distance ?? 0), 2);

            return totalDistance > 0 ? Math.Round(totalFuelCost / totalDistance, 2) : 0;

        }
    }
}
