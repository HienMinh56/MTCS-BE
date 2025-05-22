using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class ExpenseReportTypeRepository : GenericRepository<ExpenseReportType>
    {
        public ExpenseReportTypeRepository() { }
        public ExpenseReportTypeRepository(MTCSContext context) : base(context)
        {
        }
        public List<ExpenseReportType> GetAllExpenseReportTypes()
        {
            return _context.ExpenseReportTypes.ToList();
        }
        public ExpenseReportType GetExpenseReportTypeById(string id)
        {
            return _context.ExpenseReportTypes.FirstOrDefault(x => x.ReportTypeId == id);
        }
    }
}
