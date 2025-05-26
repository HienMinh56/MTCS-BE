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
    //public class FuelReportFileRepository : GenericRepository<FuelReportFile>
    //{
    //    public FuelReportFileRepository() { }

    //    public FuelReportFileRepository(MTCSContext context) => _context = context;

    //    public async Task<List<FuelReportFile>> GetFuelReportFilesByFuelReportId(string fuelReportId)
    //    {
    //        return await _context.FuelReportFiles
    //            .Where(f => f.ReportId == fuelReportId)
    //            .OrderBy(f => f.FileId)
    //            .AsNoTracking()
    //            .ToListAsync();
    //    }

    //    public async Task<FuelReportFile?> GetFuelReportFileByUrl(string url)
    //    {
    //        return await _context.FuelReportFiles
    //            .SingleOrDefaultAsync(f => f.FileUrl == url);
    //    }

    //    public async Task<List<FuelReportFile>> GetFuelReportFilesByFuelReportIds(List<string> fuelReportIds)
    //    {
    //        return await _context.FuelReportFiles
    //            .Where(f => fuelReportIds.Contains(f.ReportId))
    //            .OrderBy(f => f.FileId)
    //            .AsNoTracking()
    //            .ToListAsync();
    //    }


    //}
}
