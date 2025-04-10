using Microsoft.EntityFrameworkCore;
using MTCS.Data.Base;
using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Data.Models;

namespace MTCS.Data.Repository
{
    public class DriverRepository : GenericRepository<Driver>
    {
        public DriverRepository() : base() { }

        public DriverRepository(MTCSContext context) : base(context) { }

        public async Task<Driver?> GetDriverByEmailAsync(string email)
        {
            return await _context.Drivers
                .FirstOrDefaultAsync(d => d.Email == email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Drivers
                .AnyAsync(d => d.Email == email);
        }

        public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
        {
            return await _context.Drivers.AnyAsync(d => d.PhoneNumber == phoneNumber);
        }

        public async Task<Driver?> GetDriverByIdAsync(string driverId)
        {
            return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        }

        public async Task<string> GenerateDriverIdAsync()
        {
            const string prefix = "DRI";

            var highestId = await _context.Drivers
                .Where(d => d.DriverId.StartsWith(prefix))
                .Select(d => d.DriverId)
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


        public async Task<PagedList<ViewDriverDTO>> GetDrivers(PaginationParams paginationParams, int? status = null, string? keyword = null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = _context.Drivers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(d =>
                    d.FullName.Contains(keyword) ||
                    d.Email.Contains(keyword) ||
                    d.PhoneNumber.Contains(keyword));
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status);
            }
            else
            {
                query = query.OrderByDescending(x => x.Status);
            }

            var queryWithInclude = query.Include(d => d.DriverWeeklySummaries);

            var projectedQuery = queryWithInclude.Select(d => new ViewDriverDTO
            {
                DriverId = d.DriverId,
                FullName = d.FullName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Status = d.Status,
                TotalOrders = d.TotalProcessedOrders,
                CurrentWeekHours = d.DriverWeeklySummaries
            .Where(ws => ws.WeekStart <= today && ws.WeekEnd >= today)
            .Select(ws => ws.TotalHours.HasValue ? ws.TotalHours.Value / 60 : (int?)null)
            .FirstOrDefault()
            });

            return await PagedList<ViewDriverDTO>.CreateAsync(
                projectedQuery, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<DriverProfileDetailsDTO> GetDriverProfileDetails(string driverId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var driver = await _context.Drivers
                .AsNoTracking()
                .Where(d => d.DriverId == driverId)
                .Include(d => d.DriverDailyWorkingTimes)
                .Include(d => d.DriverWeeklySummaries)
                .Include(d => d.DriverFiles.Where(f => f.DeletedDate == null))
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return null;
            }

            // Get today's working time record (not sum)
            var todayWorkingTimeRecord = driver.DriverDailyWorkingTimes
                .FirstOrDefault(wt => wt.WorkDate == today);

            // Get today's working time in hours (or 0 if no record found)
            int dailyWorkingTimeHours = 0;
            if (todayWorkingTimeRecord != null && todayWorkingTimeRecord.TotalTime.HasValue)
            {
                // Convert minutes to hours
                dailyWorkingTimeHours = todayWorkingTimeRecord.TotalTime.Value / 60;
            }

            int? currentWeekWorkingTimeMinutes = driver.DriverWeeklySummaries
         .Where(ws => ws.WeekStart <= today && ws.WeekEnd >= today)
         .Select(ws => ws.TotalHours) // TotalHours is actually storing minutes
         .FirstOrDefault();

            // Convert minutes to hours
            int currentWeekWorkingTimeHours = 0;
            if (currentWeekWorkingTimeMinutes.HasValue)
            {
                currentWeekWorkingTimeHours = currentWeekWorkingTimeMinutes.Value / 60;
            }

            List<DriverFileDTO> files = driver.DriverFiles
                .Where(f => f.DeletedDate == null)
                .Select(f => new DriverFileDTO
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FileUrl = f.FileUrl,
                    FileType = f.FileType,
                    Description = f.Description,
                    Note = f.Note,
                    UploadDate = f.UploadDate ?? DateTime.MinValue,
                    UploadBy = f.UploadBy ?? string.Empty
                })
                .ToList();

            return new DriverProfileDetailsDTO
            {
                DriverId = driver.DriverId,
                FullName = driver.FullName,
                Email = driver.Email,
                DateOfBirth = driver.DateOfBirth,
                PhoneNumber = driver.PhoneNumber,
                Status = driver.Status,
                CreatedDate = driver.CreatedDate,
                CreatedBy = driver.CreatedBy,
                ModifiedDate = driver.ModifiedDate,
                ModifiedBy = driver.ModifiedBy,
                DailyWorkingTime = dailyWorkingTimeHours,
                CurrentWeekWorkingTime = currentWeekWorkingTimeHours,
                TotalOrder = driver.TotalProcessedOrders,
                Files = files
            };
        }

        public async Task<bool> UpdateDriverWithFiles(
    Driver updatedDriver,
    List<DriverFile> filesToAdd,
    List<string> fileIdsToRemove = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Drivers.Update(updatedDriver);

                if (filesToAdd != null && filesToAdd.Any())
                {
                    await _context.DriverFiles.AddRangeAsync(filesToAdd);
                }

                if (fileIdsToRemove != null && fileIdsToRemove.Any())
                {
                    var filesToDelete = await _context.DriverFiles
                        .Where(df => df.DriverId == updatedDriver.DriverId &&
                               fileIdsToRemove.Contains(df.FileId))
                        .ToListAsync();

                    if (filesToDelete.Any())
                    {
                        _context.DriverFiles.RemoveRange(filesToDelete);
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

        public async Task<bool> UpdateDriverFileDetails(string fileId, string description, string note, string userId)
        {
            var file = await _context.DriverFiles.FindAsync(fileId);
            if (file == null)
                return false;

            file.Description = description;
            file.Note = note;
            file.ModifiedBy = userId;
            file.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
