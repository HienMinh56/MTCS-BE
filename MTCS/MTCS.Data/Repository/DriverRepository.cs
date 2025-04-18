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
            .Select(ws => ws.TotalHours.HasValue ?
                $"{ws.TotalHours.Value / 60:D2}:{ws.TotalHours.Value % 60:D2}" : "00:00")
            .FirstOrDefault() ?? "00:00"
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
                .Include(d => d.DriverFiles
                .Where(f => f.DeletedDate == null))
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return null;
            }

            var todayWorkingTimeRecord = driver.DriverDailyWorkingTimes
                .FirstOrDefault(wt => wt.WorkDate == today);

            string dailyWorkingTime = "00:00";
            if (todayWorkingTimeRecord?.TotalTime.HasValue == true)
            {
                int totalMinutes = todayWorkingTimeRecord.TotalTime.Value;
                int hours = totalMinutes / 60;
                int minutes = totalMinutes % 60;
                dailyWorkingTime = $"{hours:D2}:{minutes:D2}";
            }

            int? weeklyTotalMinutes = driver.DriverWeeklySummaries
        .Where(ws => ws.WeekStart <= today && ws.WeekEnd >= today)
        .Select(ws => ws.TotalHours)
        .FirstOrDefault();

            string weeklyWorkingTime = "00:00";
            if (weeklyTotalMinutes.HasValue)
            {
                int hours = weeklyTotalMinutes.Value / 60;
                int minutes = weeklyTotalMinutes.Value % 60;
                weeklyWorkingTime = $"{hours:D2}:{minutes:D2}";
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
                DailyWorkingTime = dailyWorkingTime,
                CurrentWeekWorkingTime = weeklyWorkingTime,
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

        public async Task<List<Driver>> GetActiveDriversAsync()
        {
            return await _context.Drivers
                .Where(d => d.Status == 1 || d.Status == 2)
                .ToListAsync();
        }
    }
}
