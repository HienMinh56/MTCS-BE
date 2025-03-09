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

        public async Task<string> GetNextContractIdAsync()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            var existingContracts = await _context.Contracts
                .Where(c => c.ContractId.StartsWith($"CTR{timestamp}"))
                .ToListAsync();

            int nextNumber = existingContracts.Count + 1; 
            return $"CTR{timestamp}{nextNumber:D2}"; 
        }



        public async Task<List<Contract>> GetContractsAsync()
        {
            var contracts = await _context.Contracts.Include(c => c.ContractFiles).ToListAsync();
            return contracts;
        }

        public async Task<Contract> GetContractAsync(string contractId)
        {
            var contract = await _context.Contracts.Include(c => c.ContractFiles).FirstOrDefaultAsync(c => c.ContractId == contractId);
            return contract;
        }

    }
}
