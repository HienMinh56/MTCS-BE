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
    public class OrderRepository : GenericRepository<Order>
    {
        public OrderRepository()
        {
        }

        public OrderRepository(MTCSContext context) => _context = context;

        public async Task<string> GetNextCodeAsync()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string randomNumber = new Random().Next(1000, 9999).ToString();

            return $"TRAK_{timestamp}_{randomNumber}";
        }

        public IQueryable<Order> GetQueryable()
        {
            return _context.Orders.AsQueryable();
        }
    }
}
