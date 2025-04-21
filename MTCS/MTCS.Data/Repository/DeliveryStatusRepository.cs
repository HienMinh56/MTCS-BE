using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class DeliveryStatusRepository : GenericRepository<DeliveryStatus>
    {

        public DeliveryStatusRepository() { }

        public DeliveryStatusRepository(MTCSContext context) => _context = context;

        public async Task<IEnumerable<DeliveryStatus>> GetDeliveryStatusesAsync()
        {
            return await _context.DeliveryStatuses.OrderBy(d => d.StatusIndex).ToListAsync();
        }

        public async Task<DeliveryStatus> GetDeliveryStatusByIdAsync(string id)
        {
            return await _context.DeliveryStatuses.FirstOrDefaultAsync(ds => ds.StatusId == id);
        }

        public async Task<DeliveryStatus> GetSecondHighestStatusIndexAsync()
        {
            return await _context.DeliveryStatuses.OrderByDescending(ds => ds.StatusIndex).Skip(1).FirstOrDefaultAsync(s => s.IsActive == 1);
        }
        public async Task BulkUpdateAsync(List<DeliveryStatus> statuses)
        {
            foreach (var status in statuses)
            {
                var existingStatus = await _context.DeliveryStatuses
                    .FirstOrDefaultAsync(s => s.StatusId == status.StatusId);

                if (existingStatus != null)
                {
                    _context.Update(status);
                }
                else
                {
                    // Nếu chưa có, thêm mới
                    _context.DeliveryStatuses.Add(status);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
