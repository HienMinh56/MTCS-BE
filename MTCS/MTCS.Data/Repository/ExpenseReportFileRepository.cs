using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class ExpenseReportFileRepository : GenericRepository<ExpenseReportFile>
    {
        public ExpenseReportFileRepository() { }
        public ExpenseReportFileRepository(MTCSContext context) : base(context)
        {
        }
    }
}
