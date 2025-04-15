using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
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
                .Where(o => o.CreatedDate >= startDate &&
                           o.CreatedDate < periodEndDate &&
                           o.Status == "Completed")
                .ToListAsync();

            decimal totalRevenue = orders.Sum(o => o.Price ?? 0);
            int orderCount = orders.Count;
            decimal averageRevenue = orderCount > 0 ? Math.Round(totalRevenue / orderCount, 2) : 0;

            return new RevenueAnalyticsDTO
            {
                TotalRevenue = totalRevenue,
                CompletedOrders = orderCount,
                AverageRevenuePerOrder = averageRevenue,
                Period = periodLabel
            };
        }

        public async Task<List<CustomerRevenueDTO>> GetRevenueByCustomerAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == "Completed" || o.IsPay == 1);

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(o => o.CreatedDate < endDate);

            var customerGroups = await query
                .GroupBy(o => new { o.CustomerId, o.Customer.CompanyName })
                .Select(g => new CustomerRevenueDTO
                {
                    CustomerId = g.Key.CustomerId,
                    CompanyName = g.Key.CompanyName,
                    TotalRevenue = g.Sum(o => o.Price ?? 0),
                    CompletedOrders = g.Count(),
                    AverageRevenuePerOrder = g.Sum(o => o.Price ?? 0) / (g.Count() > 0 ? g.Count() : 1)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToListAsync();

            return customerGroups;
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

        public async Task<ProfitAnalyticsDTO> GetProfitAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var trips = await _context.Trips
                .Include(t => t.Order)
                .Include(t => t.FuelReports)
                .Where(t => t.Status == "completed" &&
                           t.EndTime >= startDate &&
                           t.EndTime <= endDate)
                .ToListAsync();

            decimal totalRevenue = trips.Sum(t => t.Order?.Price ?? 0);
            decimal totalFuelCost = Math.Round(trips.Sum(t => t.FuelReports.Sum(f => f.FuelCost ?? 0)), 2);
            decimal netProfit = Math.Round(totalRevenue - totalFuelCost, 2);
            decimal profitMarginPercentage = totalRevenue > 0 ? Math.Round((netProfit / totalRevenue) * 100, 2) : 0;
            return new ProfitAnalyticsDTO
            {
                TotalRevenue = totalRevenue,
                TotalFuelCost = totalFuelCost,
                NetProfit = netProfit,
                ProfitMarginPercentage = profitMarginPercentage,
                Period = $"{startDate:MM/dd/yyyy} - {endDate:MM/dd/yyyy}"
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
