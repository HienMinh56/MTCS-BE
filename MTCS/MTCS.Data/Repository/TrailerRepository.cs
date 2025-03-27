using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TrailerRepository : GenericRepository<Trailer>
    {
        public TrailerRepository() : base() { }

        public TrailerRepository(MTCSContext context) : base(context) { }

        public async Task<string> GenerateTrailerId()
        {
            const string prefix = "TRL";

            var highestId = await _context.Trailers
                .Where(t => t.TrailerId.StartsWith(prefix))
                .Select(t => t.TrailerId)
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
