using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    DateOnly? deliveryDate = null)
        {
            var query = _context.OrderDetails
                                .Include(od => od.OrderDetailFiles)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(orderId))
                query = query.Where(od => od.OrderId == orderId);

            if (!string.IsNullOrEmpty(containerNumber))
                query = query.Where(od => od.ContainerNumber.Contains(containerNumber));

            if (pickUpDate.HasValue)
                query = query.Where(od => od.PickUpDate == pickUpDate);

            if (deliveryDate.HasValue)
                query = query.Where(od => od.DeliveryDate == deliveryDate);

            query = query.OrderByDescending(od => od.DeliveryDate);


            return await query.ToListAsync();
        }
    }
}