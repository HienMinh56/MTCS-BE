using Microsoft.EntityFrameworkCore;
using MTCS.Data.Models;
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
    }
}
