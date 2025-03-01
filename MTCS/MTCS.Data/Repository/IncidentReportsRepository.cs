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

        public IncidentReportsRepository(MTCSContext context) => _context = context;

        public async Task<List<IncidentReport>> GetIncidentReportsByTripId(string tripId)
        {
            return await _context.IncidentReports.Where(i => i.TripId == tripId)
                                                 .Include(i => i.IncidentReportsFiles)
                                                 .OrderBy(i => i.ReportId)
                                                 .AsNoTracking()
                                                 .ToListAsync();
        }
    }
}