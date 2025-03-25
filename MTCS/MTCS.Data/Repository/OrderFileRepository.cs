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

       
    }
}
