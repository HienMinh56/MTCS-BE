using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs.TripsDTO;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Data.Repository
{
    public class TripRepository : GenericRepository<Trip>
    {
        public TripRepository() : base() { }

        public TripRepository(MTCSContext context) : base(context) { }


        public async Task<IEnumerable<TripDto>> GetTripsByFilterAsync(
    string? tripId,
    string? driverId,
    string? status,
    string? tractorId,
    string? trailerId,
    string? orderDetailId,
    string? trackingCode,
    string? tractorlicensePlate,
    string? trailerlicensePlate
)
        {
            var query = _context.Trips.AsQueryable();

            if (!string.IsNullOrEmpty(tripId)) query = query.Where(t => t.TripId == tripId);
            if (!string.IsNullOrEmpty(driverId)) query = query.Where(t => t.DriverId == driverId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
            if (!string.IsNullOrEmpty(tractorId)) query = query.Where(t => t.TractorId == tractorId);
            if (!string.IsNullOrEmpty(trailerId)) query = query.Where(t => t.TrailerId == trailerId);
            if (!string.IsNullOrEmpty(orderDetailId)) query = query.Where(t => t.OrderDetailId == orderDetailId);
            if (!string.IsNullOrEmpty(trackingCode)) query = query.Where(t => t.OrderDetail.Order.TrackingCode == trackingCode);
            if (!string.IsNullOrEmpty(tractorlicensePlate)) query = query.Where(t => t.Tractor.LicensePlate == tractorlicensePlate);
            if (!string.IsNullOrEmpty(trailerlicensePlate)) query = query.Where(t => t.Trailer.LicensePlate == trailerlicensePlate);

            var trips = await query
                .Include(t => t.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(t => t.Driver)
                .Include(t => t.Tractor)
                .Include(t => t.Trailer)
                .Include(t => t.TripStatusHistories)
                .Include(t => t.IncidentReports)
                    .ThenInclude(i => i.IncidentReportsFiles)
                .Include(t => t.ExpenseReports)
                    .ThenInclude(f => f.ExpenseReportFiles)
                .Include(t => t.DeliveryReports)
                    .ThenInclude(d => d.DeliveryReportsFiles)
                .AsNoTracking()
                .OrderByDescending(t => t.MatchTime)
                .ToListAsync();

            return trips.Select(t => new TripDto
            {
                TripId = t.TripId,
                OrderDetailId = t.OrderDetailId,
                OrderId = t.OrderDetail?.Order?.OrderId,
                TrackingCode = t.OrderDetail.Order?.TrackingCode,
                DriverId = t.DriverId,
                DriverName = t.Driver?.FullName,
                ContainerNumber = t.OrderDetail?.ContainerNumber,
                TractorId = t.TractorId,
                TrailerId = t.TrailerId,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Status = t.Status,
                MatchType = t.MatchType,
                MatchBy = t.MatchBy,
                MatchTime = t.MatchTime,
                Note = t.Note,
                Driver = t.Driver == null ? null : new DriverDto
                {
                    DriverId = t.Driver.DriverId,
                    FullName = t.Driver.FullName,
                    PhoneNumber = t.Driver.PhoneNumber,
                    Status = t.Driver.Status
                },
                DeliveryReports = t.DeliveryReports?.Select(dr => new DeliveryReportDto
                {
                    ReportId = dr.ReportId,
                    TripId = dr.TripId,
                    Notes = dr.Notes,
                    ReportTime = dr.ReportTime,
                    ReportBy = dr.ReportBy,
                    DeliveryReportsFiles = dr.DeliveryReportsFiles?.Select(f => new DeliveryReportFileDto
                    {
                        FileId = f.FileId,
                        ReportId = f.ReportId,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        UploadDate = f.UploadDate,
                        UploadBy = f.UploadBy,
                        Description = f.Description,
                        Note = f.Note,
                        FileUrl = f.FileUrl,
                        ModifiedDate = f.ModifiedDate,
                        ModifiedBy = f.ModifiedBy
                    }).ToList()
                }).ToList(),

                //ExpenseReports = t.ExpenseReports?.Select(fr => new ExpenseReportsDto
                //{
                //    ReportId = fr.ReportId,
                //    TripId = fr.TripId,
                //    RefuelAmount = fr.RefuelAmount,
                //    FuelCost = fr.FuelCost,
                //    Location = fr.Location,
                //    ReportTime = fr.ReportTime,
                //    ReportBy = fr.ReportBy,
                //    FuelReportFiles = fr.ExpenseReportsFiles?.Select(ff => new ExpenseReportsDtoFileDto
                //    {
                //        FileId = ff.FileId,
                //        ReportId = ff.ReportId,
                //        FileName = ff.FileName,
                //        FileType = ff.FileType,
                //        UploadDate = ff.UploadDate,
                //        UploadBy = ff.UploadBy,
                //        Description = ff.Description,
                //        Note = ff.Note,
                //        FileUrl = ff.FileUrl,
                //        ModifiedDate = ff.ModifiedDate,
                //        ModifiedBy = ff.ModifiedBy
                //    }).ToList()
                //}).ToList(),

                IncidentReports = t.IncidentReports?.Select(ir => new IncidentReportDto
                {
                    ReportId = ir.ReportId,
                    TripId = ir.TripId,
                    ReportedBy = ir.ReportedBy,
                    IncidentType = ir.IncidentType,
                    Description = ir.Description,
                    IncidentTime = ir.IncidentTime,
                    Location = ir.Location,
                    Type = ir.Type,
                    Status = ir.Status,
                    ResolutionDetails = ir.ResolutionDetails,
                    HandledBy = ir.HandledBy,
                    HandledTime = ir.HandledTime,
                    CreatedDate = ir.CreatedDate,
                    VehicleType = ir.VehicleType,
                    IncidentReportsFiles = ir.IncidentReportsFiles?.Select(iff => new IncidentReportFileDto
                    {
                        FileId = iff.FileId,
                        ReportId = iff.ReportId,
                        FileName = iff.FileName,
                        FileType = iff.FileType,
                        UploadDate = iff.UploadDate,
                        UploadBy = iff.UploadBy,
                        Description = iff.Description,
                        Note = iff.Note,
                        FileUrl = iff.FileUrl,
                        ModifiedDate = iff.ModifiedDate,
                        ModifiedBy = iff.ModifiedBy,
                        Type = iff.Type,
                        DeletedDate = iff.DeletedDate,
                        DeletedBy = iff.DeletedBy
                    }).ToList()
                }).ToList(),

                TripStatusHistories = t.TripStatusHistories?.Select(th => new TripStatusHistoryDto
                {
                    HistoryId = th.HistoryId,
                    TripId = th.TripId,
                    StatusId = th.StatusId,
                    StartTime = th.StartTime,
                    Status = th.Status?.StatusName
                }).ToList()
            }).ToList();
        }


        public async Task<(bool IsInUse, List<Trip> ActiveTrips)> IsTractorInUseStatusNow(string tractorId)
        {
            var activeTrips = await _context.Trips
                .Where(t => t.TractorId == tractorId &&
                           t.Status != "completed" &&
                           t.Status != "not_started" &&
                           t.Status != "canceled")
                .ToListAsync();

            return (activeTrips.Any(), activeTrips);
        }

        public async Task<(bool IsInUse, List<Trip> ActiveTrips)> IsTrailerInUseStatusNow(string trailerId)
        {
            var activeTrips = await _context.Trips
                .Where(t => t.TrailerId == trailerId &&
                           t.Status != "completed" &&
                           t.Status != "not_started" &&
                           t.Status != "canceled")
                .ToListAsync();

            return (activeTrips.Any(), activeTrips);
        }

        public async Task<List<Trip>> GetByDriverIdAndDateAsync(string driverId, DateOnly deliveryDate)
        {
            return await _context.Trips
                .Where(t => t.DriverId == driverId && t.OrderDetail.DeliveryDate == deliveryDate)
                .ToListAsync();
        }

        public async Task<List<Trip>> GetByDateAsync(DateOnly deliveryDate)
        {
            return await _context.Trips
                .Where(t => t.OrderDetail.DeliveryDate == deliveryDate)
                .ToListAsync();
        }

        public async Task<List<TripData>> GetAllTripsAsync()
        {
            var trips = await _context.Trips
                .Include(t => t.OrderDetail)
                .ThenInclude(od => od.Order)
                .Include(t => t.Driver)
                .OrderByDescending(t => t.MatchTime)
                .Select(t => new TripData
                {
                    TripId = t.TripId,
                    OrderDetailId = t.OrderDetailId,
                    TrackingCode = t.OrderDetail.Order.TrackingCode,
                    DriverName = t.Driver != null ? t.Driver.FullName : null,
                    DriverId = t.Driver != null ? t.Driver.DriverId : null,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    Status = t.Status,
                })
                .ToListAsync();

            return trips;
        }

        public async Task<List<TripMoResponse>> GetTripsByGroupAsync(string driverId, string groupType)
        {
            IQueryable<Trip> query = _context.Trips
                .Include(t => t.OrderDetail)
                .ThenInclude(od => od.Order)
                .OrderByDescending(t => t.MatchTime)
                .Where(t => t.DriverId == driverId);

            // Filter theo nhóm trạng thái
            switch (groupType)
            {
                case "not_started":
                    query = query.Where(t => t.Status == "not_started");
                    break;

                case "in_progress":
                    query = query.Where(t =>
                        t.Status != "not_started" &&
                        t.Status != "completed" &&
                        t.Status != "canceled");
                    break;

                case "completed":
                    query = query.Where(t =>
                        t.Status == "completed" ||
                        t.Status == "canceled");
                    break;
            }

            var trips = await query.Select(t => new TripMoResponse
            {
                TripId = t.TripId,
                TrackingCode = t.OrderDetail.Order != null ? t.OrderDetail.Order.TrackingCode : null,
                OrderDetailId = t.OrderDetailId,
                ContainerNumber = t.OrderDetail != null ? t.OrderDetail.ContainerNumber : null,
                PickUpDate = t.OrderDetail.PickUpDate,
                DeliveryDate = t.OrderDetail.DeliveryDate,
                PickUpLocation = t.OrderDetail.PickUpLocation,
                DeliveryLocation = t.OrderDetail.DeliveryLocation,
                ConReturnLocation = t.OrderDetail.ConReturnLocation,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Status = t.Status
            }).ToListAsync();

            return trips;
        }

        public async Task<bool> IsDriverHaveProcessTrip(string driverId, string? excludeTripId = null)
        {
            var query = _context.Trips.AsQueryable();

            query = query.Where(t => t.DriverId == driverId &&
                                     t.Status != "completed" &&
                                     t.Status != "not_started" &&
                                     t.Status != "canceled");

            if (!string.IsNullOrEmpty(excludeTripId))
            {
                query = query.Where(t => t.TripId != excludeTripId);
            }

            return await query.AnyAsync();
        }

        public async Task<TripTimeTableResponse> GetTripTimeTable(DateTime startOfWeek, DateTime endOfWeek)
        {
            endOfWeek = endOfWeek.Date.AddDays(1).AddSeconds(-1);

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.OrderDetail)
                .Where(t =>
                    (t.StartTime >= startOfWeek && t.StartTime <= endOfWeek) ||

                    (t.EndTime >= startOfWeek && t.EndTime <= endOfWeek) ||

                    // Trips that span the entire week (start before, end after)
                    (t.StartTime <= startOfWeek && t.EndTime >= endOfWeek) ||

                    // Trips scheduled for this week but not yet started
                    (t.Status == "not_started" &&
                     t.OrderDetail.DeliveryDate >= DateOnly.FromDateTime(startOfWeek) &&
                     t.OrderDetail.DeliveryDate <= DateOnly.FromDateTime(endOfWeek))
                );

            var trips = await query
                .Select(t => new TripTimeTable
                {
                    TripId = t.TripId,
                    TrackingCode = t.OrderDetail.Order.TrackingCode,
                    OrderDetailId = t.OrderDetailId,
                    StartTime = t.StartTime,
                    PickUpLocation = t.OrderDetail.PickUpLocation,
                    DeliveryLocation = t.OrderDetail.DeliveryLocation,
                    ConReturnLocation = t.OrderDetail.ConReturnLocation,
                    EndTime = t.EndTime,
                    DriverId = t.DriverId,
                    DriverName = t.Driver != null ? t.Driver.FullName : null,
                    Status = t.Status,
                })
                .OrderByDescending(t => t.StartTime)
                .ToListAsync();

            int completedCount = trips.Count(t => t.Status == "completed");
            int deliveringCount = trips.Count(t => t.Status != "completed" && t.Status != "not_started" &&
                                                t.Status != "canceled" && t.Status != "delaying");
            int delayingCount = trips.Count(t => t.Status == "delaying");
            int canceledCount = trips.Count(t => t.Status == "canceled");
            int notStartedCount = trips.Count(t => t.Status == "not_started");

            return new TripTimeTableResponse
            {
                Trips = trips,
                TotalCount = trips.Count,
                CompletedCount = completedCount,
                DeliveringCount = deliveringCount,
                DelayingCount = delayingCount,
                CanceledCount = canceledCount,
                NotStartedCount = notStartedCount
            };
        }
    }
}
