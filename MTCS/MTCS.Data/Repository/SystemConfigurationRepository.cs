using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
