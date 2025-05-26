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

        public ExpenseReport GetExpenseReportById(string id)
        {
            return _context.ExpenseReports
                .Include(e => e.ExpenseReportFiles)
                .FirstOrDefault(x => x.ReportId == id);
        }
    }
}
