using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TractorRepository : GenericRepository<Tractor>
    {
        public TractorRepository() : base() { }

        public TractorRepository(MTCSContext context) : base(context) { }

        public async Task<Tractor?> GetTractorById(string tractorId)
        {
            return await _context.Tractors
                .Include(t => t.TractorCate)
                .FirstOrDefaultAsync(t => t.TractorId == tractorId);
        }

        public async Task<List<TractorCategory>> GetAllCategories()
        {
            return await _context.TractorCategories
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<TractorCategory?> GetCategoryById(string categoryId)
        {
            return await _context.TractorCategories.FindAsync(categoryId);
        }

        public async Task<int> CreateCategory(TractorCategory category)
        {
            _context.TractorCategories.Add(category);
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteCategory(string categoryId)
        {
            var isInUse = await _context.Tractors.AnyAsync(t => t.TractorCateId == categoryId);
            if (isInUse)
            {
                return false;
            }

            var category = await _context.TractorCategories.FindAsync(categoryId);
            if (category != null)
            {
                _context.TractorCategories.Remove(category);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<List<Tractor>> GetTractorsByCategory(string categoryId)
        {
            return await _context.Tractors
                .Where(t => t.TractorCateId == categoryId)
                .ToListAsync();
        }

        public async Task<List<TractorCategory>> GetAllTractorCategories()
        {
            return await _context.TractorCategories.ToListAsync();
        }
    }
}