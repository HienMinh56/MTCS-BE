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

            // Get the highest DriverId that starts with the prefix
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

            // Return the new DriverId with the next number, formatted to 3 digits
            return $"{prefix}{nextNumber:D3}";
        }


        public async Task<PagedList<ViewDriverDTO>> GetDrivers(PaginationParams paginationParams, int? status = null, string? keyword = null)
        {
            var query = _context.Drivers
                .AsNoTracking();

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

            var projectedQuery = query.Select(d => new ViewDriverDTO
            {
                DriverId = d.DriverId,
                FullName = d.FullName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                Status = d.Status
            });

            return await PagedList<ViewDriverDTO>.CreateAsync(
                projectedQuery, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<(int TotalWorkingTime, int CurrentWeekWorkingTime, List<DriverFileDTO> Files)> GetDriverProfileDetails(string driverId)
        {
            var driver = await _context.Drivers
                .AsNoTracking()
                .Where(d => d.DriverId == driverId)
                .Include(d => d.DriverDailyWorkingTimes)
                .Include(d => d.DriverFiles.Where(f => f.DeletedDate == null))
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return (0, 0, new List<DriverFileDTO>());
            }

            int totalWorkingTime = driver.DriverDailyWorkingTimes
                .Where(wt => wt.TotalTime.HasValue)
                .Sum(wt => wt.TotalTime.Value);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var startOfWeek = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek));
            var endOfWeek = startOfWeek.AddDays(6);

            int currentWeekWorkingTime = driver.DriverDailyWorkingTimes
                .Where(wt => wt.WorkDate.HasValue &&
                            wt.WorkDate.Value >= startOfWeek &&
                            wt.WorkDate.Value <= endOfWeek &&
                            wt.TotalTime.HasValue)
                .Sum(wt => wt.TotalTime.Value);

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

            return (totalWorkingTime, currentWeekWorkingTime, files);
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
