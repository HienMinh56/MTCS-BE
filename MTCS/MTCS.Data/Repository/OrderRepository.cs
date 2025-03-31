using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

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
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string randomNumber = new Random().Next(1000, 9999).ToString();

            return $"TRAK_{timestamp}_{randomNumber}";
        }

        public async Task<List<Order>> GetOrdersByFiltersAsync(
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

            query = query.OrderBy(o => o.CreatedDate);

            return await query.ToListAsync();
        }
    }
}
