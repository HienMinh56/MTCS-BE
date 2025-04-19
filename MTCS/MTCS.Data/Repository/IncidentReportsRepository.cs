using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Data.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public async Task <IncidentReport?> GetIncidentReportDetails(string reportId)
        {
            return await _context.IncidentReports
                                 .Where(i => i.ReportId == reportId)
                                 .Include(i => i.Trip)
                                 .Include(i => i.IncidentReportsFiles)
                                 .SingleOrDefaultAsync();
        }

        public async Task<List<IncidentReport>> GetIncidentsByVehicleAsync(string vehicleId, int vehicleType)
        {
            IQueryable<IncidentReport> query = _context.IncidentReports
                .Include(ir => ir.Trip)
                .ThenInclude(trip => trip.Order); 

            if (vehicleType == 1) 
            {
                query = query.Where(ir =>
                    ir.VehicleType == 1 &&
                    ir.Trip != null &&
                    ir.Trip.TractorId == vehicleId);
            }
            else if (vehicleType == 2)
            {
                query = query.Where(ir =>
                    ir.VehicleType == 2 &&
                    ir.Trip != null &&
                    ir.Trip.TrailerId == vehicleId);
            }
            else
            {
                return new List<IncidentReport>(); 
            }

            return await query.ToListAsync();
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