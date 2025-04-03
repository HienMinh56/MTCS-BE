using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TrailerFileRepository : GenericRepository<TrailerFile>
    {
        public TrailerFileRepository() : base() { }

        public TrailerFileRepository(MTCSContext context) : base(context) { }

        public async Task<List<TrailerFile>> GetFilesByTrailerId(string trailerId)
        {
            return await _context.TrailerFiles
                .AsNoTracking()
                .Where(tf => tf.TrailerId == trailerId && tf.DeletedDate == null)
                .ToListAsync();
        }

        public async Task<TrailerFile?> GetFileById(string fileId)
        {
            return await _context.TrailerFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(tf => tf.FileId == fileId);
        }
    }
}
