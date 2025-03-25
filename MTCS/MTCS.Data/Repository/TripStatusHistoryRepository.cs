using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TripStatusHistoryRepository : GenericRepository<TripStatusHistory>
    {
        public TripStatusHistoryRepository() { }

        public TripStatusHistoryRepository(MTCSContext context) => _context = context;




    }
}
