using Microsoft.EntityFrameworkCore;
using MTCS.Data.Models;

namespace MTCS.Data.Helpers
{
    public class VehicleHelper
    {
        private readonly MTCSContext _context;

        public VehicleHelper(MTCSContext context)
        {
            _context = context;
        }

        public async Task<(bool Exists, string? VehicleType, string? VehicleId, string? VehicleBrand)>
            IsLicensePlateExist(string licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                return (false, null, null, null);

            var tractor = await _context.Tractors
                .AsNoTracking()
                .Where(t => t.DeletedDate == null && t.LicensePlate == licensePlate)
                .Select(t => new { t.TractorId, t.Brand })
                .FirstOrDefaultAsync();

            if (tractor != null)
            {
                return (true, "Tractor", tractor.TractorId, tractor.Brand);
            }

            var trailer = await _context.Trailers
                .AsNoTracking()
                .Where(t => t.DeletedDate == null && t.LicensePlate == licensePlate)
                .Select(t => new { t.TrailerId, t.Brand })
                .FirstOrDefaultAsync();

            if (trailer != null)
            {
                return (true, "Trailer", trailer.TrailerId, trailer.Brand);
            }

            return (false, null, null, null);
        }

        public string GetVehicleTypeVN(string vehicleType)
        {
            return vehicleType switch
            {
                "Tractor" => "Đầu kéo",
                "Trailer" => "Rơ-moóc",
                _ => vehicleType
            };
        }

        public async Task<string> GenerateTractorId()
        {
            const string prefix = "TRC";

            var highestId = await _context.Tractors
                .Where(t => t.TractorId.StartsWith(prefix))
                .Select(t => t.TractorId)
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
