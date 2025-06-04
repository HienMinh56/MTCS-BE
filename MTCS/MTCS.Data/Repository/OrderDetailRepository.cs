using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Data.Repository
{
    public class OrderDetailRepository : GenericRepository<OrderDetail>
    {
        public OrderDetailRepository()
        {
        }

        public OrderDetailRepository(MTCSContext context) => _context = context;

        public async Task<List<OrderDetail>> GetOrderDetailsByFiltersAsync(
            string? orderId = null,
            string? containerNumber = null,
            DateOnly? pickUpDate = null,
            DateOnly? deliveryDate = null,
            string? driverId = null,
            string? tripId = null)
        {
            var query = _context.OrderDetails
                                .Include(od => od.OrderDetailFiles)
                                .Include(od => od.Trips)
                                .Include(od => od.Order)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(orderId))
                query = query.Where(od => od.OrderId == orderId);

            if (!string.IsNullOrEmpty(containerNumber))
                query = query.Where(od => od.ContainerNumber.Contains(containerNumber));

            if (pickUpDate.HasValue)
                query = query.Where(od => od.PickUpDate == pickUpDate);

            if (deliveryDate.HasValue)
                query = query.Where(od => od.DeliveryDate == deliveryDate);

            if (!string.IsNullOrEmpty(driverId))
                query = query.Where(od => od.Trips.Any(t => t.DriverId == driverId));
            if (!string.IsNullOrEmpty(tripId))
                query = query.Where(od => od.Trips.Any(t => t.TripId == tripId));

            query = query.OrderByDescending(od => od.DeliveryDate);


            return await query.ToListAsync();
        }

        public async Task<OrderDetail> GetOrderDetailWithTripsAsync(string orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.Trips)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Customer)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
        }

        public async Task<bool> AnyOrderDetailDeliveringAsync(string orderId)
        {
            return await _context.OrderDetails.AnyAsync(od => od.OrderId == orderId && od.Status != "Scheduled" && od.Status != "Pending");

        }

        public async Task<bool> AreAllOrderDetailsCompletedAsync(string orderId)
        {
            return await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .AllAsync(od => od.Status == "completed");
        }

        public async Task<int> CountAsync(Expression<Func<OrderDetail, bool>> predicate)
        {
            return await _context.OrderDetails.CountAsync(predicate);
        }

        public IQueryable<OrderDetail> GetQueryable()
        {
            return _context.OrderDetails.AsQueryable();
        }
    }
}