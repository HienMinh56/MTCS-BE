using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class InternalUserRepository : GenericRepository<InternalUser>
    {
        public InternalUserRepository() : base() { }

        public InternalUserRepository(MTCSContext context) : base(context) { }

        public async Task<InternalUser?> GetUserByEmailAsync(string email)
        {
            return await _context.InternalUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.InternalUsers.AnyAsync(u => u.Email == email);
        }

        public async Task<InternalUser?> GetUserByIdAsync(string userId)
        {
            return await _context.InternalUsers.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<string?> GetUserAssignedOrder(string orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            return order?.CreatedBy;
        }

        public async Task<ProfileResponseDTO?> GetUserProfile(string userId)
        {
            var profile = await _context.InternalUsers
                .Where(p => p.UserId == userId && p.DeletedBy == null)
                .Select(p => new ProfileResponseDTO
                {
                    UserId = p.UserId,
                    FullName = p.FullName,
                    Email = p.Email,
                    PhoneNumber = p.PhoneNumber,
                    Gender = p.Gender,
                    Birthday = p.Birthday,
                    CreatedDate = p.CreatedDate,
                })
                .FirstOrDefaultAsync();

            return profile;
        }
    }
}
