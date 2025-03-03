using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository() : base() { }

        public UserRepository(MTCSContext context) : base(context) { }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
