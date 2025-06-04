using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class ExpenseReportRepository : GenericRepository<ExpenseReport>
    {
        public ExpenseReportRepository() { }
        public ExpenseReportRepository(MTCSContext context) : base(context)
        {
        }

        public List<ExpenseReport> GetExpenseReports(
        string driverId = null,
        string orderId = null,
        string tripId = null,
        string reportId = null,
        int? isPay = null)
        {
            var query = _context.ExpenseReports
                .Include(e => e.ExpenseReportFiles)
                .Include(e => e.Trip)
                .AsQueryable();

            if (!string.IsNullOrEmpty(driverId))
            {
                query = query.Where(e => e.Trip.DriverId == driverId);
            }

            if (!string.IsNullOrEmpty(orderId))
            {
                query = query.Where(e => e.Trip.OrderDetailId == orderId);
            }

            if (!string.IsNullOrEmpty(tripId))
            {
                query = query.Where(e => e.TripId == tripId);
            }

            if (!string.IsNullOrEmpty(reportId))
            {
                query = query.Where(e => e.ReportId == reportId);
            }

            if (isPay.HasValue)
            {
                query = query.Where(e => e.IsPay == isPay.Value);
            }

            return query.ToList();
        }

        public async Task<List<ExpenseReportDTO>> GetExpenseReportsList(
    string driverId = null,
    string orderId = null,
    string tripId = null,
    string reportId = null,
    int? isPay = null)
        {
            var query = _context.ExpenseReports
                .AsNoTracking()
                .Include(e => e.ReportType)
                .Include(e => e.Trip)
                .AsQueryable();

            if (!string.IsNullOrEmpty(driverId))
            {
                query = query.Where(e => e.Trip.DriverId == driverId);
            }

            if (!string.IsNullOrEmpty(orderId))
            {
                query = query.Where(e => e.Trip.OrderDetailId == orderId);
            }

            if (!string.IsNullOrEmpty(tripId))
            {
                query = query.Where(e => e.TripId == tripId);
            }

            if (!string.IsNullOrEmpty(reportId))
            {
                query = query.Where(e => e.ReportId == reportId);
            }

            if (isPay.HasValue)
            {
                query = query.Where(e => e.IsPay == isPay.Value);
            }

            var reports = await query.OrderByDescending(r => r.ReportTime).ToListAsync();

            return reports.Select(r => new ExpenseReportDTO
            {
                ReportId = r.ReportId,
                TripId = r.TripId,
                ReportTypeId = r.ReportTypeId,
                ReportTypeName = r.ReportType?.ReportType,
                Cost = r.Cost,
                Location = r.Location,
                ReportTime = r.ReportTime,
                ReportBy = r.ReportBy,
                IsPay = r.IsPay,
                Description = r.Description,

                DriverId = r.Trip?.DriverId,
                OrderDetailId = r.Trip?.OrderDetailId

            }).ToList();
        }

        public async Task<List<ExpenseReportDTO>> GetExpenseReportsDetails(
    string driverId = null,
    string orderId = null,
    string tripId = null,
    string reportId = null,
    int? isPay = null)
        {
            var query = _context.ExpenseReports
                .AsNoTracking()
                .Include(e => e.ExpenseReportFiles)
                .Include(e => e.Trip)
                    .ThenInclude(t => t.Driver)
                .Include(e => e.Trip)
                    .ThenInclude(t => t.OrderDetail)
                        .ThenInclude(od => od.Order)
                .Include(e => e.ReportType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(driverId))
            {
                query = query.Where(e => e.Trip.DriverId == driverId);
            }

            if (!string.IsNullOrEmpty(orderId))
            {
                query = query.Where(e => e.Trip.OrderDetailId == orderId);
            }

            if (!string.IsNullOrEmpty(tripId))
            {
                query = query.Where(e => e.TripId == tripId);
            }

            if (!string.IsNullOrEmpty(reportId))
            {
                query = query.Where(e => e.ReportId == reportId);
            }

            if (isPay.HasValue)
            {
                query = query.Where(e => e.IsPay == isPay.Value);
            }

            var reports = await query.ToListAsync();

            return reports.Select(r => new ExpenseReportDTO
            {
                ReportId = r.ReportId,
                TripId = r.TripId,
                ReportTypeId = r.ReportTypeId,
                ReportTypeName = r.ReportType?.ReportType,
                Cost = r.Cost,
                Location = r.Location,
                ReportTime = r.ReportTime,
                ReportBy = r.ReportBy,
                IsPay = r.IsPay,
                Description = r.Description,

                // Trip related info
                DriverId = r.Trip?.DriverId,
                DriverName = r.Trip?.Driver?.FullName,
                OrderDetailId = r.Trip?.OrderDetailId,
                TrackingCode = r.Trip?.OrderDetail?.Order?.TrackingCode,

                // Files
                ExpenseReportFiles = r.ExpenseReportFiles?.Select(f => new ExpenseReportFileDTO
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
                    DeletedDate = f.DeletedDate,
                    DeletedBy = f.DeletedBy
                }).ToList() ?? new List<ExpenseReportFileDTO>()
            }).ToList();
        }

        public ExpenseReport GetExpenseReportById(string id)
        {
            return _context.ExpenseReports
                .Include(e => e.ExpenseReportFiles)
                .FirstOrDefault(x => x.ReportId == id);
        }
    }
}
