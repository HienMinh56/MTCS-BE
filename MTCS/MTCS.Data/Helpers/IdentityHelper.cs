using Microsoft.EntityFrameworkCore;
using MTCS.Data.Models;
using MTCS.Data.Response;
using System.Security.Claims;

namespace MTCS.Data.Helpers
{
    public static class IdentityHelper
    {
        public static async Task<Dictionary<string, string>> GetUserFullNamesByIds(
            this MTCSContext context,
            IEnumerable<string> userIds)
        {
            var filteredIds = userIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (!filteredIds.Any())
                return new Dictionary<string, string>();

            return await context.InternalUsers
                .AsNoTracking()
                .Where(u => filteredIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.FullName })
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
        }

        public static List<string> CollectUserIds(
            string createdBy,
            string modifiedBy,
            string deletedBy)
        {
            var userIds = new List<string>();

            if (!string.IsNullOrEmpty(createdBy)) userIds.Add(createdBy);
            if (!string.IsNullOrEmpty(modifiedBy)) userIds.Add(modifiedBy);
            if (!string.IsNullOrEmpty(deletedBy)) userIds.Add(deletedBy);

            return userIds.Distinct().ToList();
        }

        public static string GetUserId(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrEmpty(claim.Value))
            {
                throw new InvalidOperationException("UserId not found in token");
            }
            return claim.Value;
        }
        public static string GetUserName(this ClaimsPrincipal principal)
        {
            var claim = principal.FindFirst(ClaimTypes.Name);
            if (claim == null || string.IsNullOrEmpty(claim.Value))
            {
                throw new InvalidOperationException("UserName not found in token");
            }
            return claim.Value;
        }
    }

    public class ContactHelper
    {
        private readonly MTCSContext _context;

        public ContactHelper(MTCSContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEmailInUseAsync(
    string email,
    string? excludeUserId = null)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            email = email.Trim().ToLower();

            var driverExists = await _context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.Email.ToLower() == email &&
                              (excludeUserId == null || d.DriverId != excludeUserId));

            if (driverExists)
                return true;

            var internalUserExists = await _context.InternalUsers
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == email &&
                              (excludeUserId == null || u.UserId != excludeUserId));

            return internalUserExists;
        }

        public async Task<bool> IsPhoneNumberInUseAsync(
            string phoneNumber,
            string? excludeUserId = null)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            phoneNumber = phoneNumber.Trim();

            var driverExists = await _context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.PhoneNumber == phoneNumber &&
                              (excludeUserId == null || d.DriverId != excludeUserId));

            if (driverExists)
                return true;

            var internalUserExists = await _context.InternalUsers
                .AsNoTracking()
                .AnyAsync(u => u.PhoneNumber == phoneNumber &&
                              (excludeUserId == null || u.UserId != excludeUserId));

            return internalUserExists;
        }


        public async Task<ApiResponse<bool>> ValidateContact(
    string email,
    string phoneNumber,
    string? excludeUserId = null)
        {
            var emailInUse = await IsEmailInUseAsync(email, excludeUserId);
            var phoneInUse = await IsPhoneNumberInUseAsync(phoneNumber, excludeUserId);

            if (emailInUse && phoneInUse)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Both email and phone number are already in use",
                    "Cả email và số điện thoại đều đã được sử dụng",
                    null);
            }
            else if (emailInUse)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Email is already in use",
                    "Email đã được sử dụng",
                    null);
            }
            else if (phoneInUse)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Phone number is already in use",
                    "Số điện thoại đã được sử dụng",
                    null);
            }

            return new ApiResponse<bool>(
                true,
                true,
                "Contact information is valid and unique",
                "Thông tin liên hệ hợp lệ và chưa được sử dụng",
                null);
        }

    }
}
