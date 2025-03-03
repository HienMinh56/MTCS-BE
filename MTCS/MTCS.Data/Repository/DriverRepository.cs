using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class DriverRepository : GenericRepository<Driver>
    {
        public DriverRepository() : base() { }

        public DriverRepository(MTCSContext context) : base(context) { }

        public async Task<Driver?> GetDriverByEmailAsync(string email)
        {
            return await _context.Drivers
                .FirstOrDefaultAsync(d => d.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Drivers
                .AnyAsync(d => d.Email == email);
        }

        public async Task<Driver?> GetDriverByIdAsync(string driverId)
        {
            return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        }
    }
}
