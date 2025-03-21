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
    public class DeliveryReportFileRepository : GenericRepository<DeliveryReportsFile>
    {
        public DeliveryReportFileRepository() { }
        public DeliveryReportFileRepository(MTCSContext context) => _context = context;
        public async Task<List<DeliveryReportsFile>> GetDeliveryReportFilesByDeliveryReportId(string deliveryReportId)
        {
            return await _context.DeliveryReportsFiles
                .Where(f => f.ReportId == deliveryReportId)
                .OrderBy(f => f.FileId)
                .AsNoTracking()
                .ToListAsync();
        }

    }
}
