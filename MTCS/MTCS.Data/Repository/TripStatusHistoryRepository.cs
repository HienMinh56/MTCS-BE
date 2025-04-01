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
    public class TripStatusHistoryRepository : GenericRepository<TripStatusHistory>
    {
        public TripStatusHistoryRepository() { }

        public TripStatusHistoryRepository(MTCSContext context) => _context = context;


        public async Task<TripStatusHistory> GetPreviousStatusOfTrip(string tripId)
        {
            return await _context.TripStatusHistories
        .Where(o => o.TripId == tripId)  // Lọc theo tripId trước
        .OrderByDescending(ds => ds.StartTime)  // Sắp xếp theo StartTime giảm dần
        .Skip(1)  // Bỏ qua status hiện tại
        .FirstOrDefaultAsync();  // Lấy status trước đó
        }
    }
}
