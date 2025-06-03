using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Data.Repository
{
    public class OrderRepository : GenericRepository<Order>
    {
        public OrderRepository()
        {
        }

        public OrderRepository(MTCSContext context) => _context = context;

        public async Task<string> GetNextCodeAsync()
        {
            string shortGuid = Guid.NewGuid().ToString("N").Substring(0, 4); 
            string timestamp = DateTime.Now.ToString("yyyyMMdd");
            string randomSuffix = new Random().Next(1, 99).ToString(); 
            return $"TRK{timestamp}{shortGuid}{randomSuffix}";
        }

        public async Task<List<OrderData>> GetOrdersByFiltersAsync(
            string? orderId = null,
            string? tripId = null,
            string? customerId = null,
            string? trackingCode = null,
            string? status = null)
        {
            var query = _context.Orders.Include(o => o.OrderDetails)
                                        .ThenInclude(od => od.OrderDetailFiles)
                                       .Include(o => o.Customer)
                                       .OrderByDescending(i => i.CreatedDate)
                                       .AsNoTracking()
                                       .AsQueryable();

            if (!string.IsNullOrEmpty(orderId))
                query = query.Where(o => o.OrderId == orderId);

            if (!string.IsNullOrEmpty(tripId))
            {
                query = query.Where(o => o.OrderDetails.Any(od => od.Trips.Any(t => t.TripId == tripId)));
            }

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(o => o.CustomerId == customerId);

            if (!string.IsNullOrEmpty(trackingCode))
                query = query.Where(o => o.TrackingCode == trackingCode);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            query = query.OrderByDescending(od => od.CreatedDate);


            var orders = await query.ToListAsync();

            var userIds = orders.Select(o => o.CreatedBy).Distinct().ToList();

            var users = await _context.InternalUsers
                                       .Where(u => userIds.Contains(u.UserId))
                                       .ToListAsync();

            return orders.Select(o => new OrderData
            {
                OrderId = o.OrderId,
                TrackingCode = o.TrackingCode,
                CustomerId = o.CustomerId,
                CompanyName = o.Customer?.CompanyName,
                Status = o.Status,
                Note = o.Note,
                CreatedDate = o.CreatedDate,
                CreatedBy = users.FirstOrDefault(u => u.UserId == o.CreatedBy)?.FullName,
                ModifiedDate = o.ModifiedDate,
                ModifiedBy = o.ModifiedBy,
                ContactPerson = o.ContactPerson,
                ContactPhone = o.ContactPhone,
                OrderPlacer = o.OrderPlacer,
                IsPay = o.IsPay,
                TotalAmount = o.TotalAmount,
                Quantity = o.Quantity
            }).ToList();
        }


        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.Include(o => o.Customer).ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.CreatedDate >= fromDate && o.CreatedDate <= toDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderWithDetailsTripsByTrackingCodeAsync(string trackingCode)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Trips)
                        .ThenInclude(t => t.Driver)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Trips)
                        .ThenInclude(t => t.Tractor)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Trips)
                        .ThenInclude(t => t.Trailer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Trips)
                        .ThenInclude(t => t.TripStatusHistories)
                            .ThenInclude(h => h.Status)
                .FirstOrDefaultAsync(o => o.TrackingCode == trackingCode);
        }

        public IQueryable<Order> GetQueryable()
        {
            return _context.Orders.AsQueryable();
        }

        public async Task<Order> GetOrderWithDetailsAndTripsAsync(string orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Trips)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}
