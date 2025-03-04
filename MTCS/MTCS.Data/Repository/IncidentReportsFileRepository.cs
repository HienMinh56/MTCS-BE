using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MTCS.Data.Repository
{
    public class IncidentReportsFileRepository : GenericRepository<IncidentReportsFile>
    {
        public IncidentReportsFileRepository()
        {
        }

        public IncidentReportsFileRepository(MTCSContext context) => _context = context;

        /// <summary>
        /// Get all Images
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<List<IncidentReportsFile>> GetImagesOfIncidentReport()
        {
            return await _context.IncidentReportsFiles
                .Where(i => i.DeletedBy == null)
                .OrderBy(i => i.FileId)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Get image by url
        /// </summary>
        /// <author name="Đoàn Lê Hiển Minh"></author>
        /// <returns></returns>
        public async Task<IncidentReportsFile?> GetImageByUrl(string url)
        {
            return await _context.IncidentReportsFiles
                .Where(i => i.DeletedBy == null)
                .SingleOrDefaultAsync(i => i.FileUrl == url);
        }
    }
}