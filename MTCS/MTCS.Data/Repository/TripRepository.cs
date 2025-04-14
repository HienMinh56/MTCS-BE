using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Data.Repository
{
    public class TripRepository : GenericRepository<Trip>
    {
        public TripRepository() : base() { }

        public TripRepository(MTCSContext context) : base(context) { }


        public async Task<IEnumerable<TripData>> GetTripsByFilterAsync(string? tripId, string? driverId, string? status, string? tractorId, string? trailerId, string? orderId, string? trackingCode, string? tractorlicensePlate, string? trailerlicensePlate)
        {
            var query = _context.Trips.Include(t => t.TripStatusHistories)
                                      .Include(t => t.Order)
                                      .Include(i => i.IncidentReports)
                                      .Include(t => t.FuelReports)
                                      .ThenInclude(t => t.FuelReportFiles)
                                      .Include(t => t.DeliveryReports)
                                      .Include(t => t.InspectionLogs)
                                      .AsQueryable();

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

            if (!string.IsNullOrEmpty(trackingCode))
            {
                query = query.Where(t => t.Order.TrackingCode == trackingCode);
            }

            if (!string.IsNullOrEmpty(tractorlicensePlate))
            {
                query = query.Where(t => t.Tractor.LicensePlate == tractorlicensePlate);
            }

            if (!string.IsNullOrEmpty(trailerlicensePlate))
            {
                query = query.Where(t => t.Trailer.LicensePlate == trailerlicensePlate);
            }

            var trips = await query.ToListAsync();

            // Map the IncidentReport entities to IncidentReportsData objects
            return trips.Select(t => new TripData
            {
                TripId = t.TripId,
                OrderId = t.OrderId,
                TrackingCode = t.Order?.TrackingCode,
                DriverId = t.DriverId,
                TractorId = t.TractorId,
                TrailerId = t.TrailerId,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Status = t.Status,
                MatchType = t.MatchType,
                MatchBy = t.MatchBy,
                MatchTime = t.MatchTime,
                Driver = t.Driver,
                DeliveryReports = t.DeliveryReports,
                FuelReports = t.FuelReports,
                IncidentReports = t.IncidentReports,
                InspectionLogs = t.InspectionLogs,
                TripStatusHistories = t.TripStatusHistories
            }).ToList();
        }

        public async Task<(bool IsInUse, List<Trip> ActiveTrips)> IsTractorInUseStatusNow(string tractorId)
        {
            var activeTrips = await _context.Trips
                .Where(t => t.TractorId == tractorId &&
                           t.Status != "completed" &&
                           t.Status != "not_started")
                .ToListAsync();

            return (activeTrips.Any(), activeTrips);
        }

        public async Task<(bool IsInUse, List<Trip> ActiveTrips)> IsTrailerInUseStatusNow(string trailerId)
        {
            var activeTrips = await _context.Trips
                .Where(t => t.TrailerId == trailerId &&
                           t.Status != "completed" &&
                           t.Status != "not_started")
                .ToListAsync();

            return (activeTrips.Any(), activeTrips);
        }
    }
}
