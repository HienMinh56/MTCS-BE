using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TripRepository : GenericRepository<Trip>
    {
        public TripRepository() : base() { }

        public TripRepository(MTCSContext context) : base(context) { }


        public IQueryable<Trip?> GetTripsByDriverIdAsync(string driverId)
        {
            return _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.TripStatusHistories)
                .ThenInclude(tsh => tsh.Status)
                .Where(t => t.DriverId == driverId);
        }

        public async Task<Trip?> GetTripDetailsByID(string tripId)
        {
            return await _context.Trips
                .Include(t => t.Driver)
                .Include(t => t.Order)
                .Include(t => t.Tractor)
                .Include(t => t.Trailer)
                .Include(t => t.TripStatusHistories)
                .Include(t => t.DeliveryReports)
                .Include(t => t.FuelReports)
                .Include(t => t.IncidentReports)
                .Include(t => t.InspectionLogs)
                .ThenInclude(tsh => tsh.Status)
                .FirstOrDefaultAsync(t => t.TripId == tripId);
        }
    }
}
