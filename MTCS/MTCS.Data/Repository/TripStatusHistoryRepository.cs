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
            return await _context.TripStatusHistories.OrderByDescending(ds => ds.StartTime).Skip(1).FirstOrDefaultAsync(o => o.TripId == tripId);
        }
    }
}
