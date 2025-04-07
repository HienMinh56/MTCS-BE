using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.Models;

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
    }
}
