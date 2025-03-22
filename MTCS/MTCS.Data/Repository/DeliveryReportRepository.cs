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
    public class DeliveryReportRepository : GenericRepository<DeliveryReport>
    {
        public DeliveryReportRepository() { }
        public DeliveryReportRepository(MTCSContext context) => _context = context;


        public async Task<List<DeliveryReport>> GetDeliveryReportsByDriverId(string driverId)
        {
            return await _context.DeliveryReports
                .Include(i => i.DeliveryReportsFiles)
                .Include(i => i.Trip)
                .ThenInclude(t => t.Driver)
                .Where(i => i.Trip.Driver.DriverId == driverId)
                .AsNoTracking()
                .ToListAsync();
        }


        public IEnumerable<DeliveryReport> GetDeliveryReports(string? reportId, string? tripId, string? driverid)
        {
            if (!string.IsNullOrEmpty(reportId))
            {
                var report = _context.DeliveryReports.FirstOrDefault(fr => fr.ReportId == reportId);
                return report != null ? new List<DeliveryReport> { report } : new List<DeliveryReport>();
            }

            if (!string.IsNullOrEmpty(tripId))
            {
                return _context.DeliveryReports.Where(fr => fr.TripId == tripId).ToList();
            }
            if (!string.IsNullOrEmpty(driverid))
            {
                return _context.DeliveryReports.Include(d => d.Trip).Where(fr => fr.Trip.DriverId == driverid).ToList();
            }

            return _context.DeliveryReports.ToList();
        }

        public async Task<Trip?> GetTripByReportId(string reportId)
        {
            return await _context.Trips.FirstOrDefaultAsync(t => t.TripId == reportId);
        }

    }
}
