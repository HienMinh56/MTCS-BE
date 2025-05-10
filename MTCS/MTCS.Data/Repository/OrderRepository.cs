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
            int? containerType = null,
            string? containerNumber = null,
            string? trackingCode = null,
            string? status = null,
            DateOnly? pickUpDate = null,
            DateOnly? deliveryDate = null)
        {
            var query = _context.Orders.Include(o => o.OrderFiles)
                                       .Include(o => o.Customer)
                                       .OrderByDescending(i => i.CreatedDate)
                                       .AsNoTracking()
                                       .AsQueryable();

            if (!string.IsNullOrEmpty(orderId))
                query = query.Where(o => o.OrderId == orderId);

            if (!string.IsNullOrEmpty(tripId))
                query = query.Include(o => o.Trips).Where(o => o.Trips.Any(t => t.TripId == tripId));

            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(o => o.CustomerId == customerId);

            if (containerType.HasValue)
                query = query.Where(o => o.ContainerType == containerType.Value);

            if (!string.IsNullOrEmpty(containerNumber))
                query = query.Where(o => o.ContainerNumber == containerNumber);

            if (!string.IsNullOrEmpty(trackingCode))
                query = query.Where(o => o.TrackingCode == trackingCode);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            if (pickUpDate.HasValue)
                query = query.Where(o => o.PickUpDate == DateOnly.FromDateTime(pickUpDate.Value.ToDateTime(TimeOnly.MinValue)));

            if (deliveryDate.HasValue)
                query = query.Where(o => o.DeliveryDate == DateOnly.FromDateTime(deliveryDate.Value.ToDateTime(TimeOnly.MinValue)));

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
                Temperature = o.Temperature,
                Weight = o.Weight,
                PickUpDate = o.PickUpDate,
                DeliveryDate = o.DeliveryDate,
                Status = o.Status,
                Note = o.Note,
                CreatedDate = o.CreatedDate,
                CreatedBy = users.FirstOrDefault(u => u.UserId == o.CreatedBy)?.FullName,
                ModifiedDate = o.ModifiedDate,
                ModifiedBy = o.ModifiedBy,
                ContainerType = o.ContainerType,
                PickUpLocation = o.PickUpLocation,
                DeliveryLocation = o.DeliveryLocation,
                ConReturnLocation = o.ConReturnLocation,
                DeliveryType = o.DeliveryType,
                Price = o.Price,
                ContainerNumber = o.ContainerNumber,
                ContactPerson = o.ContactPerson,
                ContactPhone = o.ContactPhone,
                OrderPlacer = o.OrderPlacer,
                Distance = o.Distance,
                ContainerSize = o.ContainerSize,
                IsPay = o.IsPay,
                CompletionTime = o.CompletionTime,
                OrderFiles = o.OrderFiles,
            }).ToList();
        }


        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.Include(o => o.Customer).ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByDateRangeAsync(DateOnly fromDate, DateOnly toDate)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.DeliveryDate >= fromDate && o.DeliveryDate <= toDate)
                .ToListAsync();
        }

        public async Task<Order> GetByTrackingCodeAsync(string trackingCode)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Trips)
                    .ThenInclude(t => t.Driver)
                .Include(o => o.Trips)
                    .ThenInclude(t => t.Tractor)
                .Include(o => o.Trips)
                    .ThenInclude(t => t.Trailer)
                .Include(o => o.Trips)
                    .ThenInclude(t => t.TripStatusHistories)
                        .ThenInclude(h => h.Status)
                .FirstOrDefaultAsync(o => o.TrackingCode == trackingCode);
        }

        public IQueryable<Order> GetQueryable()
        {
            return _context.Orders.AsQueryable();
        }

        public async Task<int> CountOrdersByDeliveryDateAsync(DateOnly deliveryDate)
        {
            return await _context.Orders
                .Where(o => o.DeliveryDate == deliveryDate)
                .CountAsync();
        }

        public async Task<Order> GetOrderWithTripsAsync(string orderId)
        {
            return await _context.Orders
                .Include(o => o.Trips)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}
