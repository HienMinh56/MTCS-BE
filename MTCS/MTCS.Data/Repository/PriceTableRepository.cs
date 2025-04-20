using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;
using MTCS.Data.Request;

namespace MTCS.Data.Repository
{
    public class PriceTableRepository : GenericRepository<PriceTable>
    {
        public PriceTableRepository() { }
        public PriceTableRepository(MTCSContext context) : base(context) { }
        public async Task<List<PriceTable>> GetActivePriceTables()
        {
            return await _context.PriceTables
                .AsNoTracking()
                .Where(p => p.Status == 1)
                .OrderBy(p => p.MinKm)
                .ToListAsync();
        }

        public async Task<int?> GetMaxVersion()
        {
            return await _context.PriceTables
                .AsNoTracking()
                .Select(p => p.Version)
                .MaxAsync();
        }

        public async Task<List<PriceTable>> GetPriceTablesByCurrentMaxVersion()
        {
            var maxVersion = await _context.PriceTables
                .AsNoTracking()
                .MaxAsync(p => p.Version) ?? 0;

            return await _context.PriceTables
                .AsNoTracking()
                .Where(p => p.Version == maxVersion)
                .ToListAsync();
        }

        public async Task<List<PriceTable>> GetPriceTables(int? version = null)
        {
            var query = _context.PriceTables.AsNoTracking();
            var maxVersion = await _context.PriceTables
                .AsNoTracking()
                .MaxAsync(p => p.Version) ?? 0;

            if (!version.HasValue)
            {
                query = query.Where(p => p.Version == maxVersion && p.Status == 1);
            }
            else if (version.Value == maxVersion)
            {
                query = query.Where(p => p.Version == version.Value && p.Status == 1);
            }
            else
            {
                query = query.Where(p => p.Version == version.Value);
            }
            return await query
                .OrderBy(p => p.MinKm)
                .ToListAsync();
        }

        public async Task<(List<VersionInfo> VersionsInfo, int ActiveVersion)> GetPriceTableVersions()
        {
            var allVersions = await _context.PriceTables
                .AsNoTracking()
                .Select(p => p.Version ?? 0)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            var activeVersion = await _context.PriceTables
                .AsNoTracking()
                .Where(p => p.Status == 1)
                .MaxAsync(p => p.Version ?? 0);

            var versionsInfo = new List<VersionInfo>();

            foreach (var version in allVersions)
            {
                var startDate = await _context.PriceTables
                    .AsNoTracking()
                    .Where(p => p.Version == version)
                    .MinAsync(p => p.CreatedDate);

                DateTime? endDate = null;
                if (version < activeVersion)
                {
                    endDate = await _context.PriceTables
                        .AsNoTracking()
                        .Where(p => p.Version == version && p.Status == 0)
                        .MaxAsync(p => p.ModifiedDate);
                }

                versionsInfo.Add(new VersionInfo
                {
                    Version = version,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }

            return (versionsInfo, activeVersion);
        }


        public async Task<PriceTable?> GetPriceForCalculation(double distance, int containerType, int containerSize, int deliveryType)
        {
            return await _context.PriceTables
                .AsNoTracking()
                .Where(pt => pt.Status == 1 &&
                       pt.ContainerType == containerType &&
                       pt.ContainerSize == containerSize &&
                       pt.DeliveryType == deliveryType &&
                       pt.MinKm <= distance &&
                       pt.MaxKm >= distance)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PriceChangeGroup>> GetPriceChangesInVersion(int version)
        {
            var allPrices = await _context.PriceTables
                .AsNoTracking()
                .Where(p => p.Version == version)
                .ToListAsync();

            var changeGroups = allPrices
                .GroupBy(p => new
                {
                    p.ContainerSize,
                    p.ContainerType,
                    p.DeliveryType,
                    p.MinKm,
                    p.MaxKm
                })
                .Where(g => g.Count() > 1)
                .Select(g => new PriceChangeGroup
                {
                    ContainerSize = g.Key.ContainerSize ?? 0,
                    ContainerType = g.Key.ContainerType ?? 0,
                    DeliveryType = g.Key.DeliveryType ?? 0,
                    MinKm = g.Key.MinKm ?? 0,
                    MaxKm = g.Key.MaxKm ?? 0,
                    Changes = g.OrderByDescending(p => p.CreatedDate).ToList()
                })
                .ToList();

            return changeGroups;
        }



    }
}
