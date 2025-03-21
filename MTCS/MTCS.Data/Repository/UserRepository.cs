using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class UserRepository : GenericRepository<Customer>
    {
        public UserRepository() : base() { }

        public UserRepository(MTCSContext context) : base(context) { }

        public async Task<Customer?> GetUserByEmailAsync(string email)
        {
            return await _context.Customers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Customers.AnyAsync(u => u.Email == email);
        }

        public async Task<Customer?> GetUserByIdAsync(string userId)
        {
            return await _context.Customers.FirstOrDefaultAsync(u => u.CustomerId == userId);
        }

        public async Task<string?> GetUserAssignedOrder(string orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            return order?.CreatedBy;
        }
    }
}
