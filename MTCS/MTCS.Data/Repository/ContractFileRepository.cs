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

        public async Task<string> GetNextFileNumberAsync()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            var existingFiles = await _context.ContractFiles
                .Where(c => c.FileId.StartsWith($"CTR{timestamp}"))
                .ToListAsync();

            int nextNumber = existingFiles.Count + 1;
            return $"FIL{timestamp}{nextNumber:D2}";
        }
    }
}
