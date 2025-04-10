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
    public class DriverDailyWorkingTimeRepository : GenericRepository<DriverDailyWorkingTime>
    {
        public DriverDailyWorkingTimeRepository() : base() { }

        public DriverDailyWorkingTimeRepository(MTCSContext context) : base(context) { }

        public async Task<List<DriverDailyWorkingTime>> GetAllAsync()
        {
            return await _context.DriverDailyWorkingTimes.ToListAsync();
        }

        public async Task<DriverDailyWorkingTime?> GetByDriverIdAndDateAsync(string driverId, DateOnly workDate)
        {
            return await _context.DriverDailyWorkingTimes
                .FirstOrDefaultAsync(x => x.DriverId == driverId && x.WorkDate == workDate);
        }

        public async Task<List<DriverDailyWorkingTime>> GetByDriverIdAndDateRangeAsync(string driverId, DateOnly fromDate, DateOnly toDate)
        {
            return await _context.DriverDailyWorkingTimes
                .Where(x => x.DriverId == driverId && x.WorkDate >= fromDate && x.WorkDate <= toDate)
                .ToListAsync();
        }
    }
}