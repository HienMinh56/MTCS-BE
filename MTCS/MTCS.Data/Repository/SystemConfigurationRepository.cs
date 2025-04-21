
using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class SystemConfigurationRepository : GenericRepository<SystemConfiguration>
    {
        public SystemConfigurationRepository() : base() { }

        public SystemConfigurationRepository(MTCSContext context) : base(context) { }

        public async Task<List<SystemConfiguration>> GetAllAsync()
        {
            return await _context.SystemConfigurations.ToListAsync();
        }
        public async Task<SystemConfiguration> GetConfigByKey(string key)
        {
            return await _context.SystemConfigurations
                .FirstOrDefaultAsync(x => x.ConfigKey == key);
        }
    }
}
