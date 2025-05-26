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
    public class OrderDetailFileRepository : GenericRepository<OrderDetailFile>
    {
        public OrderDetailFileRepository()
        {
        }

        public OrderDetailFileRepository(MTCSContext context) => _context = context;

        public async Task<OrderDetailFile?> GetImageByUrl(string url)
        {
            return await _context.OrderDetailFiles
                .Where(i => i.DeletedBy == null)
                .SingleOrDefaultAsync(i => i.FileUrl == url);
        }
    }
}
