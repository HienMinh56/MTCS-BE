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

        public async Task<PagedList<DriverUseHistory>> GetDriverUsageHistory(
    string driverId,
    PaginationParams paginationParams)
        {
            var query = _context.Trips
                .AsNoTracking()
                .Include(x => x.Tractor)
                .Include(x => x.Trailer)
                .Join(
                    _context.DeliveryStatuses,
                    trip => trip.Status,
                    status => status.StatusId,
                    (trip, status) => new { Trip = trip, Status = status })
                .Where(x => x.Trip.DriverId == driverId)
                .OrderByDescending(x => x.Trip.MatchTime)
                .Select(x => new DriverUseHistory
                {
                    TripId = x.Trip.TripId,
                    TractorId = x.Trip.TractorId,
                    TractorPlate = x.Trip.Tractor.LicensePlate,
                    TrailerId = x.Trip.TrailerId,
                    TrailerPlate = x.Trip.Trailer.LicensePlate,
                    StartTime = x.Trip.StartTime,
                    EndTime = x.Trip.EndTime,
                    MatchBy = x.Trip.MatchBy,
                    MatchTime = x.Trip.MatchTime,
                    Status = x.Status.StatusName
                });

            return await PagedList<DriverUseHistory>.CreateAsync(
                query,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<DriverTimeTableResponse> GetDriverTimeTable(string driverId, DateTime startOfWeek, DateTime endOfWeek)
        {
            endOfWeek = endOfWeek.Date.AddDays(1).AddSeconds(-1);
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Get driver details including weekly working time
            var driver = await _context.Drivers
                .AsNoTracking()
                .Include(d => d.DriverDailyWorkingTimes)
                .Include(d => d.DriverWeeklySummaries)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver == null)
                return new DriverTimeTableResponse();

            // Get weekly working time
            var weeklyRecord = driver.DriverWeeklySummaries
                .FirstOrDefault(ws => ws.WeekStart <= DateOnly.FromDateTime(startOfWeek) &&
                                     ws.WeekEnd >= DateOnly.FromDateTime(endOfWeek));

            string totalWorkingTime = "00:00";
            if (weeklyRecord?.TotalHours.HasValue == true)
            {
                int hours = weeklyRecord.TotalHours.Value / 60;
                int minutes = weeklyRecord.TotalHours.Value % 60;
                totalWorkingTime = $"{hours:D2}:{minutes:D2}";
            }

            // Get today's working time
            var dailyRecord = driver.DriverDailyWorkingTimes
                .FirstOrDefault(wt => wt.WorkDate == today);

            string dailyWorkingTime = "00:00";
            if (dailyRecord?.TotalTime.HasValue == true)
            {
                int hours = dailyRecord.TotalTime.Value / 60;
                int minutes = dailyRecord.TotalTime.Value % 60;
                dailyWorkingTime = $"{hours:D2}:{minutes:D2}";
            }

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(t => t.Tractor)
                .Include(t => t.Trailer)
                .Where(t => t.DriverId == driverId)
                .Where(t =>
                    // Trips that start within the selected week
                    (t.StartTime >= startOfWeek && t.StartTime <= endOfWeek) ||

                    // Trips that end within the selected week
                    (t.EndTime >= startOfWeek && t.EndTime <= endOfWeek) ||

                    // Trips that span the entire week (start before, end after)
                    (t.StartTime <= startOfWeek && t.EndTime >= endOfWeek) ||

                    // Trips scheduled for this week but not yet started
                    (t.Status == "not_started" &&
                     t.OrderDetail.DeliveryDate >= DateOnly.FromDateTime(startOfWeek) &&
                     t.OrderDetail.DeliveryDate <= DateOnly.FromDateTime(endOfWeek))
                );

            var trips = await query
                .Select(t => new DriverTimeTable
                {
                    TripId = t.TripId,
                    TrackingCode = t.OrderDetail.Order.TrackingCode,
                    OrderDetailId = t.OrderDetailId,
                    TractorId = t.TractorId,
                    TractorPlate = t.Tractor.LicensePlate,
                    TrailerId = t.TrailerId,
                    TrailerPlate = t.Trailer.LicensePlate,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    Status = t.Status,
                    EstimatedCompletionTime = t.OrderDetail.CompletionTime
                })
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            // Calculate counts
            int completedCount = trips.Count(t => t.Status == "completed");
            int deliveringCount = trips.Count(t => t.Status != "completed" && t.Status != "not_started" &&
                                              t.Status != "canceled" && t.Status != "delaying");
            int delayingCount = trips.Count(t => t.Status == "delaying");
            int canceledCount = trips.Count(t => t.Status == "canceled");
            int notStartedCount = trips.Count(t => t.Status == "not_started");

            return new DriverTimeTableResponse
            {
                DriverSchedule = trips,
                TotalCount = trips.Count,
                CompletedCount = completedCount,
                DeliveringCount = deliveringCount,
                DelayingCount = delayingCount,
                CanceledCount = canceledCount,
                NotStartedCount = notStartedCount,
                WeeklyWorkingTime = totalWorkingTime,
            };
        }
        public async Task<List<DriverTimeTableResponse>> GetAllDriversTimeTable(DateTime startOfWeek, DateTime endOfWeek)
        {
            endOfWeek = endOfWeek.Date.AddDays(1).AddSeconds(-1);
            var weekStart = DateOnly.FromDateTime(startOfWeek);
            var weekEnd = DateOnly.FromDateTime(endOfWeek);

            var tripsQuery = _context.Trips
                .AsNoTracking()
                .Include(t => t.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(t => t.Tractor)
                .Include(t => t.Trailer)
                .Where(t =>
                    // Trips that start within the selected week
                    (t.StartTime >= startOfWeek && t.StartTime <= endOfWeek) ||

                    // Trips that end within the selected week
                    (t.EndTime >= startOfWeek && t.EndTime <= endOfWeek) ||

                    // Trips that span the entire week (start before, end after)
                    (t.StartTime <= startOfWeek && t.EndTime >= endOfWeek) ||

                    // Trips scheduled for this week but not yet started
                    (t.Status == "not_started" &&
                     t.OrderDetail.DeliveryDate >= weekStart &&
                     t.OrderDetail.DeliveryDate <= weekEnd)
                );

            // Get distinct driver IDs from the trips
            var driverIds = await tripsQuery.Select(t => t.DriverId).Distinct().ToListAsync();

            // For each driver, construct their time table
            var result = new List<DriverTimeTableResponse>();

            foreach (var driverId in driverIds)
            {
                var driver = await _context.Drivers
                    .AsNoTracking()
                    .Include(d => d.DriverDailyWorkingTimes)
                    .Include(d => d.DriverWeeklySummaries)
                    .FirstOrDefaultAsync(d => d.DriverId == driverId);

                if (driver == null)
                    continue;

                // Get all trips for this driver in the specified week
                var allDriverTrips = await tripsQuery
                    .Where(t => t.DriverId == driverId)
                    .Select(t => new
                    {
                        t.TripId,
                        t.OrderDetailId,
                        t.StartTime,
                        t.EndTime,
                        t.Status,
                        TrackingCode = t.OrderDetail.Order.TrackingCode,
                        CompletionTime = t.OrderDetail.CompletionTime,
                        DeliveryDate = t.OrderDetail.DeliveryDate,
                        t.TractorId,
                        TractorPlate = t.Tractor.LicensePlate,
                        t.TrailerId,
                        TrailerPlate = t.Trailer.LicensePlate
                    })
                    .ToListAsync();

                // Calculate daily working times
                var dailyWorkingTimes = new List<DailyWorkingTimeDTO>();
                int totalPracticalMinutes = 0;
                int totalExpectedMinutes = 0;

                for (var day = startOfWeek.Date; day <= endOfWeek.Date; day = day.AddDays(1))
                {
                    var currentDay = DateOnly.FromDateTime(day);

                    // Get trips for this day
                    var tripsOnDay = allDriverTrips.Where(t =>
                        (t.StartTime?.Date == day.Date) ||
                        (t.EndTime?.Date == day.Date) ||
                        (t.Status == "not_started" && t.DeliveryDate == currentDay)).ToList();

                    // Calculate expected time (sum of CompletionTime for all trips assigned on this day)
                    int expectedMinutes = 0;
                    foreach (var trip in tripsOnDay.Where(t => t.Status != "canceled"))
                    {
                        if (trip.CompletionTime.HasValue)
                        {
                            expectedMinutes += (trip.CompletionTime.Value.Hour * 60) + trip.CompletionTime.Value.Minute;
                        }
                    }

                    // Calculate practical time (actual time spent on completed trips)
                    int practicalMinutes = 0;
                    foreach (var trip in tripsOnDay.Where(t => t.Status == "completed" && t.StartTime.HasValue && t.EndTime.HasValue))
                    {
                        // Calculate only the portion of time spent on this day
                        var tripStart = trip.StartTime.Value;
                        var tripEnd = trip.EndTime.Value;

                        // If trip spans multiple days, calculate only this day's portion
                        if (tripStart.Date < day.Date)
                            tripStart = day.Date;

                        if (tripEnd.Date > day.Date)
                            tripEnd = day.Date.AddDays(1).AddSeconds(-1);

                        var duration = (tripEnd - tripStart).TotalMinutes;
                        practicalMinutes += (int)duration;
                    }

                    // Format times as HH:MM
                    string expectedTimeDisplay = FormatMinutesToHHMM(expectedMinutes);
                    string practicalTimeDisplay = FormatMinutesToHHMM(practicalMinutes);

                    // Add to totals
                    totalExpectedMinutes += expectedMinutes;
                    totalPracticalMinutes += practicalMinutes;

                    dailyWorkingTimes.Add(new DailyWorkingTimeDTO
                    {
                        Date = currentDay,
                        WorkingTime = practicalTimeDisplay,
                        TotalMinutes = practicalMinutes,
                        ExpectedWorkingTime = expectedTimeDisplay,
                        ExpectedMinutes = expectedMinutes
                    });
                }

                // Format weekly totals
                string weeklyWorkingTime = FormatMinutesToHHMM(totalPracticalMinutes);
                string expectedWeeklyWorkingTime = FormatMinutesToHHMM(totalExpectedMinutes);

                // Convert trips to DTO format
                var driverTrips = allDriverTrips.Select(t => new DriverTimeTable
                {
                    TripId = t.TripId,
                    TrackingCode = t.TrackingCode,
                    OrderDetailId = t.OrderDetailId,
                    TractorId = t.TractorId,
                    TractorPlate = t.TractorPlate,
                    TrailerId = t.TrailerId,
                    TrailerPlate = t.TrailerPlate,
                    DeliveryDate = t.DeliveryDate,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    Status = t.Status,
                    EstimatedCompletionTime = t.CompletionTime
                }).OrderBy(t => t.StartTime).ToList();

                int completedCount = driverTrips.Count(t => t.Status == "completed");
                int deliveringCount = driverTrips.Count(t => t.Status != "completed" && t.Status != "not_started" &&
                                                  t.Status != "canceled" && t.Status != "delaying");
                int delayingCount = driverTrips.Count(t => t.Status == "delaying");
                int canceledCount = driverTrips.Count(t => t.Status == "canceled");
                int notStartedCount = driverTrips.Count(t => t.Status == "not_started");

                result.Add(new DriverTimeTableResponse
                {
                    DriverId = driverId,
                    DriverName = driver.FullName,
                    DriverSchedule = driverTrips,
                    TotalCount = driverTrips.Count,
                    CompletedCount = completedCount,
                    DeliveringCount = deliveringCount,
                    DelayingCount = delayingCount,
                    CanceledCount = canceledCount,
                    NotStartedCount = notStartedCount,
                    WeeklyWorkingTime = weeklyWorkingTime,
                    TotalWeeklyMinutes = totalPracticalMinutes,
                    ExpectedWeeklyWorkingTime = expectedWeeklyWorkingTime,
                    ExpectedWeeklyMinutes = totalExpectedMinutes,
                    DailyWorkingTimes = dailyWorkingTimes
                });
            }
            return result;
        }

        private string FormatMinutesToHHMM(int totalMinutes)
        {
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours:D2}:{minutes:D2}";
        }
    }
}
