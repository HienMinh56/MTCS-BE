using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MTCS.Data.Repository
{
    public class CustomerRepository : GenericRepository<Customer>
    {
        public CustomerRepository()
        {
        }

        public CustomerRepository(MTCSContext context) => _context = context;

        public async Task<List<Customer>> GetAllCustomer(string? customerId, string? companyName)
        {
            var query = _context.Customers.Include(i => i.Contracts)
                                          .Include(i => i.Orders)
                                          .AsNoTracking()
                                          .AsQueryable();

            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(i => i.CustomerId == customerId);
            }

            if (!string.IsNullOrEmpty(companyName))
            {
                query = query.Where(i => i.CompanyName == companyName);
            }

            return await query.ToListAsync();
        }

        public async Task<Customer?> GetCustomerByCompanyNameAsync(string companyName)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.CompanyName == companyName);
        }
        public async Task<string> GenerateCustomerIdAsync()
        {
            const string prefix = "CUS";

            // Get the highest CustomerId that starts with the prefix
            var highestId = await _context.Customers
                .Where(c => c.CustomerId.StartsWith(prefix))
                .Select(c => c.CustomerId)
                .OrderByDescending(id => id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(highestId) && highestId.Length > prefix.Length)
            {
                var numericPart = highestId.Substring(prefix.Length);
                if (int.TryParse(numericPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            // Return the new CustomerId with the next number, formatted to 4 digits
            return $"{prefix}{nextNumber:D3}";
        }

    }
}
