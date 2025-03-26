using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class PriceTableRepository : GenericRepository<PriceTable>
    {
        public PriceTableRepository() { }
        public PriceTableRepository(MTCSContext context) : base(context)
        {
        }
    }
}
