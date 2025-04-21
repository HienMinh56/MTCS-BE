using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Data.Repository
{
    public class IncidentReportsRepository : GenericRepository<IncidentReport>
    {
        public IncidentReportsRepository()
        {
        }

        public async Task<List<IncidentReportsData>> GetAllIncidentReport(string? driverId, string? tripId, string? reportId)
        {
            var query = _context.IncidentReports.Include(i => i.IncidentReportsFiles)
                                                .Include(i => i.Trip)
                                                .ThenInclude(t => t.Driver)
                                                .Include(i => i.Trip)
                                                .ThenInclude(i => i.Order)
                                                .OrderByDescending(i => i.CreatedDate)
                                                .AsNoTracking()
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

            var incidentReports = await query.ToListAsync();

            // Map the IncidentReport entities to IncidentReportsData objects
            return incidentReports.Select(i => new IncidentReportsData
            {
                ReportId = i.ReportId,
                TripId = i.TripId,
                TrackingCode = i.Trip?.Order?.TrackingCode,
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
                IncidentReportsFiles = i.IncidentReportsFiles,
                Trip = i.Trip
            }).ToList();
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