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

        public async Task<Driver?> GetDriverByIdAsync(string driverId)
        {
            return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        }

        public async Task<PagedList<ViewDriverDTO>> GetDrivers(PaginationParams paginationParams, int? status = null)
        {
            var query = _context.Drivers
                .AsNoTracking()
                .Select(d => new ViewDriverDTO
                {
                    DriverId = d.DriverId,
                    FullName = d.FullName,
                    Email = d.Email,
                    CreatedBy = d.CreatedBy,
                    Status = d.Status
                });

            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }

            return await PagedList<ViewDriverDTO>.CreateAsync(
                query, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<(int TotalWorkingTime, int CurrentWeekWorkingTime, List<string> FileUrls)> GetDriverProfileDetails(string driverId)
        {
            var driver = await _context.Drivers
                .AsNoTracking()
                .Where(d => d.DriverId == driverId)
                .Include(d => d.DriverDailyWorkingTimes)
                .Include(d => d.DriverFiles.Where(f => f.DeletedDate == null))
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return (0, 0, new List<string>());
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

            List<string> fileUrls = driver.DriverFiles
                .Where(f => !string.IsNullOrEmpty(f.FileUrl))
                .Select(f => f.FileUrl)
                .ToList();

            return (totalWorkingTime, currentWeekWorkingTime, fileUrls);
        }
    }
}
