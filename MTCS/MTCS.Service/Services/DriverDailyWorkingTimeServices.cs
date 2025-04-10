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
    public interface IDriverDailyWorkingTimeService
    {
        Task<BusinessResult> GetTotalTimeByDriverAndDateAsync(string driverId, DateOnly workDate);
        Task<BusinessResult> GetTotalTimeByRangeAsync(string driverId, DateOnly fromDate, DateOnly toDate);
    }
    public class DriverDailyWorkingTimeService : IDriverDailyWorkingTimeService

    {
        private readonly UnitOfWork _unitOfWork;

        public DriverDailyWorkingTimeService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessResult> GetTotalTimeByDriverAndDateAsync(string driverId, DateOnly workDate)
        {
            var record = await _unitOfWork.DriverDailyWorkingTimeRepository
                .GetByDriverIdAndDateAsync(driverId, workDate);

            int totalMinutes = record?.TotalTime ?? 0;

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            string timeFormatted = $"{hours} giờ {minutes} phút";

            return new BusinessResult(Const.SUCCESS_READ_CODE, "Lấy dữ liệu thành công", timeFormatted);
        }

        public async Task<BusinessResult> GetTotalTimeByRangeAsync(string driverId, DateOnly fromDate, DateOnly toDate)
        {
            var records = await _unitOfWork.DriverDailyWorkingTimeRepository
                .GetByDriverIdAndDateRangeAsync(driverId, fromDate, toDate);

            int totalMinutes = records.Sum(r => r.TotalTime ?? 0);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            string data = $"{hours} giờ {minutes} phút";

            return new BusinessResult(Const.SUCCESS_READ_CODE, "Lấy tổng thời gian thành công", data );
        }
    }
}
