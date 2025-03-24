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
                .FirstOrDefaultAsync(t => t.TractorId == tractorId);
        }

        public async Task<bool> LicensePlateExist(string licensePlate)
        {
            return await _context.Tractors
                .AsNoTracking()
                .AnyAsync(t => t.LicensePlate == licensePlate);
        }

        public async Task<List<Tractor>> GetTractorsByContainerType(int containerType)
        {
            return await _context.Tractors
                .Where(t => t.ContainerType == containerType)
                .ToListAsync();
        }

        public async Task<List<Tractor>> GetAllTractorsByContainerTypes(int[] containerTypes)
        {
            return await _context.Tractors
                .Where(t => t.ContainerType.HasValue && containerTypes.Contains(t.ContainerType.Value))
                .ToListAsync();
        }
    }
}