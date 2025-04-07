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
    public class OrderFileRepository : GenericRepository<OrderFile>
    {
        public OrderFileRepository()
        {
        }

        public OrderFileRepository(MTCSContext context) => _context = context;

        public async Task<OrderFile?> GetImageByUrl(string url)
        {
            return await _context.OrderFiles
                .Where(i => i.DeletedBy == null)
                .SingleOrDefaultAsync(i => i.FileUrl == url);
        }
    }
}
