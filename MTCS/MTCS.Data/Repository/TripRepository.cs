using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TripRepository : GenericRepository<Trip>
    {
        public TripRepository() : base() { }

        public TripRepository(MTCSContext context) : base(context) { }


        public async Task<IEnumerable<Trip>> GetTripsByFilterAsync(string? tripId, string? driverId, string? status, string? tractorId, string? trailerId, string? orderId)
        {
            var query = _context.Trips.Include(t => t.TripStatusHistories).AsQueryable();

            if (!string.IsNullOrEmpty(tripId))
            {
                query = query.Where(t => t.TripId == tripId);
            }

            if (!string.IsNullOrEmpty(driverId))
            {
                query = query.Where(t => t.DriverId == driverId);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }
            if (!string.IsNullOrEmpty(tractorId))
            {
                query = query.Where(t => t.TractorId == tractorId);
            }
            if (!string.IsNullOrEmpty(trailerId))
            {
                query = query.Where(t => t.TrailerId == trailerId);
            }
            if (!string.IsNullOrEmpty(orderId))
            {
                query = query.Where(t => t.OrderId == orderId);
            }

            return await query.ToListAsync();
        }
    }
}
