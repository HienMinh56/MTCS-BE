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
    public class DriverWeeklySummaryRepository : GenericRepository<DriverWeeklySummary>
    {
        public DriverWeeklySummaryRepository() : base() { }

        public DriverWeeklySummaryRepository(MTCSContext context) : base(context) { }

        public async Task<List<DriverWeeklySummary>> GetAllAsync()
        {
            return await _context.DriverWeeklySummaries.ToListAsync();
        }

        public async Task<DriverWeeklySummary?> GetByDriverIdAndWeekAsync(string driverId, DateOnly weekStart, DateOnly weekEnd)
        {
            return await _context.DriverWeeklySummaries
                .FirstOrDefaultAsync(x => x.DriverId == driverId && x.WeekStart == weekStart && x.WeekEnd == weekEnd);
        }


    }
}
