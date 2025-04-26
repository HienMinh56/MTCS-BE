using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs.IncidentReportDTO;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Data.Repository
{
    public class IncidentReportsRepository : GenericRepository<IncidentReport>
    {
        public IncidentReportsRepository()
        {
        }

        public async Task<List<IncidentReportDTO>> GetAllIncidentReport(string? driverId, string? tripId, string? reportId)
        {
            var query = _context.IncidentReports
                .Include(i => i.IncidentReportsFiles)
                .Include(i => i.Trip)
                    .ThenInclude(t => t.Driver)
                .Include(i => i.Trip)
                    .ThenInclude(t => t.Order)
                        .ThenInclude(o => o.Trips) // <-- thêm Include Trips
                .AsQueryable();

            if (!string.IsNullOrEmpty(driverId))
            {
                query = query.Where(i => i.Trip.DriverId == driverId);
            }
            if (!string.IsNullOrEmpty(tripId))
            {
                query = query.Where(i => i.TripId == tripId);
            }
            if (!string.IsNullOrEmpty(reportId))
            {
                query = query.Where(i => i.ReportId == reportId);
            }

            var incidentReports = await query
                .Select(i => new IncidentReportDTO
                {
                    ReportId = i.ReportId,
                    TripId = i.TripId,
                    TrackingCode = i.Trip.Order.TrackingCode,
                    ReportedBy = i.ReportedBy,
                    IncidentType = i.IncidentType,
                    Description = i.Description,
                    IncidentTime = i.IncidentTime,
                    Location = i.Location,
                    Type = i.Type,
                    VehicleType = i.VehicleType,
                    Status = i.Status,
                    ResolutionDetails = i.ResolutionDetails,
                    HandledBy = i.HandledBy,
                    HandledTime = i.HandledTime,
                    CreatedDate = i.CreatedDate,
                    IncidentReportsFiles = i.IncidentReportsFiles.Select(f => new IncidentReportsFileDTO
                    {
                        FileId = f.FileId,
                        FileName = f.FileName,
                        FileUrl = f.FileUrl
                    }).ToList(),
                    Trip = i.Trip == null ? null : new TripDTO
                    {
                        TripId = i.Trip.TripId,
                        OrderId = i.Trip.OrderId,
                        DriverId = i.Trip.DriverId,
                        TractorId = i.Trip.TractorId,
                        TrailerId = i.Trip.TrailerId,
                        StartTime = i.Trip.StartTime,
                        EndTime = i.Trip.EndTime,
                        Status = i.Trip.Status,
                        MatchType = i.Trip.MatchType,
                        MatchBy = i.Trip.MatchBy,
                        MatchTime = i.Trip.MatchTime,
                        Note = i.Trip.Note,
                        Driver = i.Trip.Driver == null ? null : new DriverDTO
                        {
                            DriverId = i.Trip.Driver.DriverId,
                            FullName = i.Trip.Driver.FullName,
                            PhoneNumber = i.Trip.Driver.PhoneNumber,
                            Email = i.Trip.Driver.Email,
                            Status = i.Trip.Driver.Status,
                            CreatedDate = i.Trip.Driver.CreatedDate
                        }
                    },
                    Driver = i.Trip.Driver == null ? null : new DriverDTO
                    {
                        DriverId = i.Trip.Driver.DriverId,
                        FullName = i.Trip.Driver.FullName,
                        PhoneNumber = i.Trip.Driver.PhoneNumber,
                        Email = i.Trip.Driver.Email,
                        Status = i.Trip.Driver.Status,
                        CreatedDate = i.Trip.Driver.CreatedDate
                    },
                    Order = i.Trip.Order == null ? null : new OrderDTO
                    {
                        OrderId = i.Trip.Order.OrderId,
                        TrackingCode = i.Trip.Order.TrackingCode,
                        CustomerId = i.Trip.Order.CustomerId,
                        Temperature = i.Trip.Order.Temperature,
                        Weight = i.Trip.Order.Weight,
                        PickUpDate = i.Trip.Order.PickUpDate,
                        DeliveryDate = i.Trip.Order.DeliveryDate,
                        Status = i.Trip.Order.Status,
                        Note = i.Trip.Order.Note,
                        CreatedDate = i.Trip.Order.CreatedDate,
                        CreatedBy = i.Trip.Order.CreatedBy,
                        ModifiedDate = i.Trip.Order.ModifiedDate,
                        ModifiedBy = i.Trip.Order.ModifiedBy,
                        ContainerType = i.Trip.Order.ContainerType,
                        PickUpLocation = i.Trip.Order.PickUpLocation,
                        DeliveryLocation = i.Trip.Order.DeliveryLocation,
                        ConReturnLocation = i.Trip.Order.ConReturnLocation,
                        DeliveryType = i.Trip.Order.DeliveryType,
                        Price = i.Trip.Order.Price,
                        ContainerNumber = i.Trip.Order.ContainerNumber,
                        ContactPerson = i.Trip.Order.ContactPerson,
                        ContactPhone = i.Trip.Order.ContactPhone,
                        OrderPlacer = i.Trip.Order.OrderPlacer,
                        Distance = i.Trip.Order.Distance,
                        ContainerSize = i.Trip.Order.ContainerSize,
                        IsPay = i.Trip.Order.IsPay,
                        CompletionTime = i.Trip.Order.CompletionTime,
                        Trips = i.Trip.Order.Trips.Select(t => new TripDTO
                        {
                            TripId = t.TripId,
                            OrderId = t.OrderId,
                            DriverId = t.DriverId,
                            TractorId = t.TractorId,
                            TrailerId = t.TrailerId,
                            StartTime = t.StartTime,
                            EndTime = t.EndTime,
                            Status = t.Status,
                            MatchType = t.MatchType,
                            MatchBy = t.MatchBy,
                            MatchTime = t.MatchTime,
                            Note = t.Note
                        }).ToList()
                    }
                }).ToListAsync();

            return incidentReports;
        }

        public async Task<string> GetNextIncidentCodeAsync()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return $"INC{timestamp}";
        }

        /// <summary>
        /// Get incident reports by trip id
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<List<IncidentReport>> GetIncidentReportsByTripId(string tripId)
        {
            return await _context.IncidentReports
                                 .Where(i => i.TripId == tripId)
                                 .Include(i => i.IncidentReportsFiles)
                                 .OrderBy(i => i.ReportId)
                                 .AsNoTracking()
                                 .ToListAsync();
        }


        /// <summary>
        /// Get images by report id
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IncidentReport?> GetImagesByReportId(string reportId)
        {
            return await _context.IncidentReports
                                 .Where(i => i.ReportId == reportId)
                                 .Include(i => i.IncidentReportsFiles)
                                 .AsNoTracking()
                                 .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Get incident report details by report id
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IncidentReport?> GetIncidentReportDetails(string reportId)
        {
            return await _context.IncidentReports
                                 .Where(i => i.ReportId == reportId)
                                 .Include(i => i.Trip)
                                 .Include(i => i.IncidentReportsFiles)
                                 .SingleOrDefaultAsync();
        }

        public async Task<List<IncidentReport>> GetIncidentsByVehicleAsync(string vehicleId, int vehicleType)
        {
            IQueryable<string> incidentIds = null;

            if (vehicleType == 1) // Tractor
            {
                incidentIds = _context.IncidentReports
                    .Where(ir => ir.VehicleType == 1 && ir.Trip != null && ir.Trip.TractorId == vehicleId)
                    .Select(ir => ir.ReportId);
            }
            else if (vehicleType == 2) // Trailer
            {
                incidentIds = _context.IncidentReports
                    .Where(ir => ir.VehicleType == 2 && ir.Trip != null && ir.Trip.TrailerId == vehicleId)
                    .Select(ir => ir.ReportId);
            }
            else
            {
                return new List<IncidentReport>();
            }

            var incidents = await _context.IncidentReports
                .Where(ir => incidentIds.Contains(ir.ReportId))
                .OrderByDescending(ir => ir.IncidentTime)
                .Select(ir => new IncidentReport
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
                    IncidentReportsFiles = ir.IncidentReportsFiles.Select(f => new IncidentReportsFile
                    {
                        FileId = f.FileId,
                        ReportId = f.ReportId,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        UploadDate = f.UploadDate,
                        UploadBy = f.UploadBy,
                        Description = f.Description,
                        Note = f.Note,
                        DeletedDate = f.DeletedDate,
                        DeletedBy = f.DeletedBy,
                        FileUrl = f.FileUrl,
                        ModifiedDate = f.ModifiedDate,
                        ModifiedBy = f.ModifiedBy,
                        Type = f.Type
                    }).ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            return incidents;
        }

        public async Task<IncidentReport> GetRecentIncidentReport(string tripId)
        {
            var recentIncidentReport = await _context.IncidentReports
                .OrderByDescending(i => i.CreatedDate)
                .FirstOrDefaultAsync(t => t.TripId == tripId);
            if (recentIncidentReport != null)
            {
                return recentIncidentReport;
            }
            else
            {
                throw new Exception("No incident report found.");
            }
        }
    }
}