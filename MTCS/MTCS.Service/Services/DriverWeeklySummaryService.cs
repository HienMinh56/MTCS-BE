using MTCS.Common;
using MTCS.Data;
using MTCS.Service.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCS.Service.Services
{
    public interface IDriverWeeklySummaryService
    {
        Task<BusinessResult> GetWeeklyWorkingTimeAsync(string driverId);
    }

    public class DriverWeeklySummaryService : IDriverWeeklySummaryService
    {
        private readonly UnitOfWork _unitOfWork;

        public DriverWeeklySummaryService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessResult> GetWeeklyWorkingTimeAsync(string driverId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            var weeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                .GetByDriverIdAndWeekAsync(driverId, weekStart, weekEnd);

            if (weeklyRecord == null || weeklyRecord.TotalHours == null)
                return new BusinessResult(Const.SUCCESS_READ_CODE, "Không có dữ liệu tuần này", "0 giờ 0 phút");

            var totalTime = weeklyRecord.TotalHours.Value;
            var hours = totalTime.Hour;
            var minutes = totalTime.Minute;

            var data = $"{hours} giờ {minutes} phút";

            return new BusinessResult(Const.SUCCESS_READ_CODE, "Lấy thời gian làm việc tuần này thành công", data);
        }
    }
}

