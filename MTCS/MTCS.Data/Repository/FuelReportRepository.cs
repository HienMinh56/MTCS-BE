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
    //public class FuelReportRepository : GenericRepository<FuelReport>
    //{
    //   public FuelReportRepository() { }

    //    public FuelReportRepository(MTCSContext context) => _context = context;


    //    public async Task<List<FuelReport>> GetFuelReportsByDriverId(string driverId)
    //    {
    //        return await _context.FuelReports
    //            .Where(f => f.ReportBy == driverId)
    //            .OrderBy(f => f.ReportId)
    //            .AsNoTracking()
    //            .ToListAsync();
    //    }

    //    public async Task<FuelReport?> GetFuelReportById(string reportId)
    //    {
    //        return await _context.FuelReports
    //            .SingleOrDefaultAsync(f => f.ReportId == reportId);
    //    }

    //    public async Task<List<FuelReport>> GetFuelReportsByTripId(string tripId)
    //    {
    //        return await _context.FuelReports
    //            .Where(f => f.TripId == tripId)
    //            .OrderBy(f => f.ReportId)
    //            .AsNoTracking()
    //            .ToListAsync();
    //    }

    //    public IEnumerable<FuelReport> GetFuelReports(string? reportId, string? tripId, string? driverId)
    //    {
    //        IQueryable<FuelReport> query = _context.FuelReports
    //            .Include(f => f.Trip)
    //            .Include(f => f.FuelReportFiles)
    //            .OrderByDescending(f => f.ReportTime);

    //        if (!string.IsNullOrEmpty(reportId))
    //        {
    //            var report = query.FirstOrDefault(fr => fr.ReportId == reportId);
    //            return report != null ? new List<FuelReport> { report } : new List<FuelReport>();
    //        }

    //        if (!string.IsNullOrEmpty(tripId))
    //        {
    //            return query.Where(fr => fr.TripId == tripId).ToList();
    //        }

    //        if (!string.IsNullOrEmpty(driverId))
    //        {
    //            return query.Where(fr => fr.Trip.DriverId == driverId).ToList();
    //        }

    //        return query.ToList();
    //    }


        //public async Task<Order> GetOrderByTripId(string tripId)
        //{
        //    var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == tripId);
        //    return await _context.Orders
        //        .Include(o => o.Trips)
        //        .FirstOrDefaultAsync(o => o.OrderId == trip.OrderId);
        //}
    }
//}
