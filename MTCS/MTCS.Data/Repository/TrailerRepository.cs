using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class TrailerRepository : GenericRepository<Trailer>
    {
        public TrailerRepository() : base() { }

        public TrailerRepository(MTCSContext context) : base(context) { }

        public async Task<Trailer?> GetTrailerById(string trailerId)
        {
            return await _context.Trailers
                .FirstOrDefaultAsync(t => t.TrailerId == trailerId);
        }

        public async Task<List<Trailer>> GetTrailersByContainerSize(int containerSize)
        {
            return await _context.Trailers
                .Where(t => t.ContainerSize == containerSize)
                .ToListAsync();
        }

        public async Task<List<Trailer>> GetAllTrailersByContainerSizes(int[] containerSizes)
        {
            return await _context.Trailers
                .Where(t => t.ContainerSize.HasValue && containerSizes.Contains(t.ContainerSize.Value))
                .ToListAsync();
        }

        public async Task<TrailerBasicInfoResultDTO> GetTrailersBasicInfo(
            PaginationParams paginationParams,
            string? searchKeyword = null,
            TrailerStatus? status = null,
            bool? maintenanceDueSoon = null,
            bool? registrationExpiringSoon = null,
            int? maintenanceDueDays = null,
            int? registrationExpiringDays = null)
        {
            var baseQuery = _context.Trailers
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                searchKeyword = searchKeyword.Trim().ToLower();
                baseQuery = baseQuery.Where(t =>
                    t.LicensePlate.ToLower().Contains(searchKeyword)
                );
            }

            // Aggregate counts
            int allCount = await baseQuery.CountAsync();
            int activeCount = await baseQuery
                .Where(t => t.Status == TrailerStatus.Active.ToString())
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
                    .OrderByDescending(t => t.Status == TrailerStatus.Active.ToString())
                    .ThenByDescending(t => t.CreatedDate)
                    .ThenBy(t => t.TrailerId);
            }

            var projectedQuery = query.Select(t => new TrailerBasicDTO
            {
                TrailerId = t.TrailerId,
                LicensePlate = t.LicensePlate,
                Brand = t.Brand,
                Status = t.Status,
                ContainerSize = t.ContainerSize.HasValue ? (ContainerSize)t.ContainerSize.Value : null,
                NextMaintenanceDate = t.NextMaintenanceDate,
                RegistrationExpirationDate = t.RegistrationExpirationDate
            });

            var pagedList = await PagedList<TrailerBasicDTO>.CreateAsync(
                projectedQuery,
                paginationParams.PageNumber,
                paginationParams.PageSize);

            return new TrailerBasicInfoResultDTO
            {
                Trailers = pagedList,
                AllCount = allCount,
                ActiveCount = activeCount,
                MaintenanceDueCount = maintenanceDueCount,
                RegistrationExpiryDueCount = registrationExpiryDueCount
            };
        }

        public async Task<TrailerDetailsDTO> GetTrailerDetailsById(string trailerId)
        {
            var trailer = await _context.Trailers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TrailerId == trailerId);

            if (trailer == null)
                return null;

            var userIds = IdentityHelper.CollectUserIds(
                trailer.CreatedBy,
                trailer.ModifiedBy,
                trailer.DeletedBy);

            var users = await _context.GetUserFullNamesByIds(userIds);

            var orderCount = await _context.Trips
                .AsNoTracking()
                .Where(trip => trip.TrailerId == trailerId)
                .Select(trip => trip.OrderId)
                .Distinct()
                .CountAsync();

            return new TrailerDetailsDTO
            {
                TrailerId = trailer.TrailerId,
                LicensePlate = trailer.LicensePlate,
                Brand = trailer.Brand,
                ManufactureYear = trailer.ManufactureYear,
                MaxLoadWeight = trailer.MaxLoadWeight,
                LastMaintenanceDate = trailer.LastMaintenanceDate,
                NextMaintenanceDate = trailer.NextMaintenanceDate,
                RegistrationDate = trailer.RegistrationDate,
                RegistrationExpirationDate = trailer.RegistrationExpirationDate,
                Status = trailer.Status,
                ContainerSize = trailer.ContainerSize.HasValue ? (ContainerSize)trailer.ContainerSize.Value : null,
                CreatedDate = trailer.CreatedDate,
                ModifiedDate = trailer.ModifiedDate,
                DeletedDate = trailer.DeletedDate,
                OrderCount = orderCount,

                CreatedBy = users.TryGetValue(trailer.CreatedBy ?? "", out var createdBy) ? createdBy : null,
                ModifiedBy = users.TryGetValue(trailer.ModifiedBy ?? "", out var modifiedBy) ? modifiedBy : null,
                DeletedBy = users.TryGetValue(trailer.DeletedBy ?? "", out var deletedBy) ? deletedBy : null,
                Files = new List<TrailerFileDTO>()
            };
        }

        public async Task<bool> UpdateTrailerWithFiles(
    Trailer updatedTrailer,
    List<TrailerFile> filesToAdd,
    List<string> fileIdsToRemove = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Trailers.Update(updatedTrailer);

                if (filesToAdd != null && filesToAdd.Any())
                {
                    await _context.TrailerFiles.AddRangeAsync(filesToAdd);
                }

                if (fileIdsToRemove != null && fileIdsToRemove.Any())
                {
                    var filesToDelete = await _context.TrailerFiles
                        .Where(tf => tf.TrailerId == updatedTrailer.TrailerId &&
                               fileIdsToRemove.Contains(tf.FileId))
                        .ToListAsync();

                    if (filesToDelete.Any())
                    {
                        _context.TrailerFiles.RemoveRange(filesToDelete);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> UpdateTrailerFileDetails(string fileId, string description, string note, string userId)
        {
            var file = await _context.TrailerFiles.FindAsync(fileId);
            if (file == null)
                return false;

            file.Description = description;
            file.Note = note;
            file.ModifiedBy = userId;
            file.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
