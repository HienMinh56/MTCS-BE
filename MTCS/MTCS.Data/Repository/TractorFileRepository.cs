using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TractorFileRepository : GenericRepository<TractorFile>
    {
        public TractorFileRepository() : base() { }

        public TractorFileRepository(MTCSContext context) : base(context) { }

        public async Task<List<TractorFile>> GetFilesByTractorId(string tractorId)
        {
            return await _context.TractorFiles
                .AsNoTracking()
                .Where(tf => tf.TractorsId == tractorId && tf.DeletedDate == null)
                .ToListAsync();
        }

        public async Task<TractorFile?> GetFileById(string fileId)
        {
            return await _context.TractorFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(tf => tf.FileId == fileId);
        }
    }
}
