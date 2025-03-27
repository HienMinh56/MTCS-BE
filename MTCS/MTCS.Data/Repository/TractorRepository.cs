using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TractorRepository : GenericRepository<Tractor>
    {
        public TractorRepository() : base() { }

        public TractorRepository(MTCSContext context) : base(context) { }

        public async Task<Tractor?> GetTractorById(string tractorId)
        {
            return await _context.Tractors
                .FirstOrDefaultAsync(t => t.TractorId == tractorId);
        }

        public async Task<bool> LicensePlateExist(string licensePlate)
        {
            return await _context.Tractors
                .AsNoTracking()
                .AnyAsync(t => t.LicensePlate == licensePlate);
        }

        public async Task<List<Tractor>> GetTractorsByContainerType(int containerType)
        {
            return await _context.Tractors
                .Where(t => t.ContainerType == containerType)
                .ToListAsync();
        }

        public async Task<List<Tractor>> GetAllTractorsByContainerTypes(int[] containerTypes)
        {
            return await _context.Tractors
                .Where(t => t.ContainerType.HasValue && containerTypes.Contains(t.ContainerType.Value))
                .ToListAsync();
        }

        public async Task<TractorBasicInfoResultDTO> GetTractorsBasicInfo(
    PaginationParams paginationParams,
    TractorStatus? status = null,
    bool? maintenanceDueSoon = null,
    bool? registrationExpiringSoon = null,
    int? maintenanceDueDays = null,
    int? registrationExpiringDays = null)
        {
            var baseQuery = _context.Tractors
                .AsNoTracking()
                .Where(t => t.DeletedDate == null);

            // Aggregate counts
            int allCount = await baseQuery.CountAsync();
            int activeCount = await baseQuery
                .Where(t => t.Status == TractorStatus.Active.ToString())
                .CountAsync();

            int maintenanceThresholdDays = maintenanceDueDays ?? 7;
            DateTime now = DateTime.Now;
            DateTime maintenanceThreshold = now.AddDays(maintenanceThresholdDays);
            int maintenanceDueCount = await baseQuery
                .Where(t => t.NextMaintenanceDate.HasValue &&
                            t.NextMaintenanceDate.Value >= now &&
                            t.NextMaintenanceDate.Value <= maintenanceThreshold)
                .CountAsync();

            int registrationThresholdDays = registrationExpiringDays ?? 30;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var registrationThreshold = today.AddDays(registrationThresholdDays);
            int registrationExpiryDueCount = await baseQuery
                .Where(t => t.RegistrationExpirationDate.HasValue &&
                            t.RegistrationExpirationDate.Value >= today &&
                            t.RegistrationExpirationDate.Value <= registrationThreshold)
                .CountAsync();

            var query = baseQuery;

            if (status.HasValue)
            {
                string statusString = status.ToString();
                query = query.Where(t => t.Status == statusString);
            }

            if (maintenanceDueSoon == true || maintenanceDueDays.HasValue)
            {
                var days = maintenanceDueDays ?? 7;
                var dateThreshold = DateTime.Now.AddDays(days);
                query = query.Where(t => t.NextMaintenanceDate.HasValue &&
                                         t.NextMaintenanceDate.Value <= dateThreshold &&
                                         t.NextMaintenanceDate.Value >= DateTime.Now);
            }

            if (registrationExpiringSoon == true || registrationExpiringDays.HasValue)
            {
                var days = registrationExpiringDays ?? 30;
                var todayOnly = DateOnly.FromDateTime(DateTime.Today);
                var expirationThreshold = todayOnly.AddDays(days);
                query = query.Where(t => t.RegistrationExpirationDate.HasValue &&
                                         t.RegistrationExpirationDate.Value <= expirationThreshold &&
                                         t.RegistrationExpirationDate.Value >= todayOnly);
            }

            if (!status.HasValue)
            {
                query = query
                    .OrderByDescending(t => t.Status == TractorStatus.Active.ToString())
                    .ThenByDescending(t => t.CreatedDate)
                    .ThenBy(t => t.TractorId);
            }

            var projectedQuery = query.Select(t => new TractorBasicDTO
            {
                TractorId = t.TractorId,
                LicensePlate = t.LicensePlate,
                Brand = t.Brand,
                Status = t.Status,
                ContainerType = t.ContainerType.HasValue ? (ContainerType)t.ContainerType.Value : null,
                NextMaintenanceDate = t.NextMaintenanceDate,
                RegistrationExpirationDate = t.RegistrationExpirationDate
            });

            var pagedList = await PagedList<TractorBasicDTO>.CreateAsync(
                projectedQuery,
                paginationParams.PageNumber,
                paginationParams.PageSize);

            return new TractorBasicInfoResultDTO
            {
                Tractors = pagedList,
                AllCount = allCount,
                ActiveCount = activeCount,
                MaintenanceDueCount = maintenanceDueCount,
                RegistrationExpiryDueCount = registrationExpiryDueCount
            };
        }

        public async Task<TractorDetailsDTO> GetTractorDetailsById(string tractorId)
        {
            var tractor = await _context.Tractors
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TractorId == tractorId);

            if (tractor == null)
                return null;

            var userIds = IdentityHelper.CollectUserIds(
                tractor.CreatedBy,
                tractor.ModifiedBy,
                tractor.DeletedBy);

            var users = await _context.GetUserFullNamesByIds(userIds);

            var orderCount = await _context.Trips
                .AsNoTracking()
                .Where(trip => trip.TractorId == tractorId)
                .Select(trip => trip.OrderId)
                .Distinct()
                .CountAsync();

            return new TractorDetailsDTO
            {
                TractorId = tractor.TractorId,
                LicensePlate = tractor.LicensePlate,
                Brand = tractor.Brand,
                ManufactureYear = tractor.ManufactureYear,
                MaxLoadWeight = tractor.MaxLoadWeight,
                LastMaintenanceDate = tractor.LastMaintenanceDate,
                NextMaintenanceDate = tractor.NextMaintenanceDate,
                RegistrationDate = tractor.RegistrationDate,
                RegistrationExpirationDate = tractor.RegistrationExpirationDate,
                Status = tractor.Status,
                ContainerType = tractor.ContainerType.HasValue ? (ContainerType)tractor.ContainerType.Value : null,
                CreatedDate = tractor.CreatedDate,
                ModifiedDate = tractor.ModifiedDate,
                DeletedDate = tractor.DeletedDate,
                OrderCount = orderCount,

                CreatedBy = users.TryGetValue(tractor.CreatedBy ?? "", out var createdBy) ? createdBy : null,
                ModifiedBy = users.TryGetValue(tractor.ModifiedBy ?? "", out var modifiedBy) ? modifiedBy : null,
                DeletedBy = users.TryGetValue(tractor.DeletedBy ?? "", out var deletedBy) ? deletedBy : null
            };
        }
    }
}
