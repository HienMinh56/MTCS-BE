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
    public class ContractFileRepository : GenericRepository<ContractFile>
    {
        public ContractFileRepository()
        {
        }

        public ContractFileRepository(MTCSContext context) => _context = context;

        public async Task<int> GetNextFileNumberAsync()
        {
            var lastFile = await _context.ContractFiles
                .OrderByDescending(c => c.ContractId)
                .FirstOrDefaultAsync();

            if (lastFile == null)
                return 1; 

            string lastNumberStr = lastFile.FileId.Substring(3); 
            if (int.TryParse(lastNumberStr, out int lastNumber))
            {
                return lastNumber + 1;
            }

            return 1; 
        }
    }
}
