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
    }
}
