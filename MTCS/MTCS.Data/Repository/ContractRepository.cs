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
    public class ContractRepository : GenericRepository<Contract>
    {
        public ContractRepository()
        {
        }

        public ContractRepository(MTCSContext context) => _context = context;

        public async Task<int> GetNextContractNumberAsync()
        {
            var lastContract = await _context.Contracts
                .OrderByDescending(c => c.ContractId)
                .FirstOrDefaultAsync();

            if (lastContract == null)
                return 1; 

            string lastNumberStr = lastContract.ContractId.Substring(3); 
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                return lastNumber + 1;
            }

            return 1; 
        }
    }
}
