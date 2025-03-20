using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TrailerRepository : GenericRepository<Trailer>
    {
        public TrailerRepository() : base() { }

        public TrailerRepository(MTCSContext context) : base(context) { }

        //public async Task<Trailer?> GetTrailerById(string trailerId)
        //{
        //    return await _context.Trailers
        //        .Include(t => t.TrailerCate)
        //        .FirstOrDefaultAsync(t => t.TrailerId == trailerId);
        //}

        //public async Task<int> CreateTrailerCategory(TrailerCategory category)
        //{
        //    _context.TrailerCategories.Add(category);
        //    return await _context.SaveChangesAsync();
        //}

        //public async Task<List<TrailerCategory>> GetAllTrailerCategories()
        //{
        //    return await _context.TrailerCategories.ToListAsync();
        //}

        //public async Task<TrailerCategory?> GetCategoryById(string categoryId)
        //{
        //    return await _context.TrailerCategories.FindAsync(categoryId);
        //}

    }
}
