using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
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

        public async Task<List<InternalUser>> GetStaffList()
        {
            return await _context.InternalUsers
                .Where(u => u.Role == 1 && u.DeletedBy == null)
                .ToListAsync();
        }

        public async Task<PagedList<InternalUser>> GetInternalUserWithFilter(
            PaginationParams paginationParams,
            string? keyword = null,
            int? role = null)
        {
            var query = _context.InternalUsers
                .AsNoTracking();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query
                    .Where(u => u.FullName.Contains(keyword) ||
                                u.Email.Contains(keyword) ||
                                u.PhoneNumber.Contains(keyword));
            }

            query = query.OrderByDescending(x => x.Status)
                          .ThenByDescending(x => x.CreatedDate);

            return await PagedList<InternalUser>.CreateAsync(query, paginationParams.PageNumber, paginationParams.PageSize);
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

        public async Task<string> GenerateStaffIdAsync()
        {
            const string prefix = "STF";

            var highestId = await _context.InternalUsers
                .Where(d => d.UserId.StartsWith(prefix) && d.Role == (int)InternalUserRole.Staff)
                .Select(d => d.UserId)
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

            return $"{prefix}{nextNumber:D3}";
        }

        public async Task<string> GenerateAdminIdAsync()
        {
            const string prefix = "ADM";

            var highestId = await _context.InternalUsers
                .Where(d => d.UserId.StartsWith(prefix) && d.Role == (int)InternalUserRole.Admin)
                .Select(d => d.UserId)
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

            return $"{prefix}{nextNumber:D3}";
        }

    }
}
