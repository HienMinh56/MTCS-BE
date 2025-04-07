using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class DriverFileRepository : GenericRepository<DriverFile>
    {
        public DriverFileRepository() : base() { }

        public DriverFileRepository(MTCSContext context) : base(context) { }

        public async Task<List<DriverFile>> GetFilesByDriverId(string driverId)
        {
            return await _context.DriverFiles
                .AsNoTracking()
                .Where(tf => tf.DriverId == driverId && tf.DeletedDate == null)
                .ToListAsync();
        }

        public async Task<DriverFile?> GetFileById(string fileId)
        {
            return await _context.DriverFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(tf => tf.FileId == fileId);
        }
    }
}
