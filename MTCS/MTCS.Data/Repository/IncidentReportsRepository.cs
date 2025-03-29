using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
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

        public async Task<List<IncidentReport>> GetAllIncidentReport(string? driverId, string? tripId, string? reportId)
        {
            var query = _context.IncidentReports.Include(i => i.IncidentReportsFiles)
                                                .Include(i => i.Trip)
                                                .ThenInclude(t => t.Driver)
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

            return await query.ToListAsync();
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
    }
}