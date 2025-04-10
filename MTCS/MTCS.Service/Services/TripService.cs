using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Request;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace MTCS.Service.Services
{
    public class TripService : ITripService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public TripService(UnitOfWork unitOfWork, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        #region GetTripsByFilter
        public async Task<BusinessResult> GetTripsByFilterAsync(string? tripId, string? driverId, string? status, string? tractorId, string? trailerId, string? orderId, string? trackingCode, string? tractorlicensePlate, string? trailerlicensePlate)
        {
            try
            {
                var trips = await _unitOfWork.TripRepository.GetTripsByFilterAsync(tripId,driverId, status, tractorId, trailerId, orderId, trackingCode, tractorlicensePlate, trailerlicensePlate);

                if (trips == null)
                {
                    return new BusinessResult(404, "Not Found");
                }

                return new BusinessResult(200, "Success", trips);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);
            }
        }
        #endregion

        #region UpdateStatusTrip
        public async Task<BusinessResult> UpdateStatusTrip(string tripId, string newStatusId, string userId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var trip =  _unitOfWork.TripRepository.Get(t => t.TripId == tripId);
                if (trip == null) return new BusinessResult(404, "Trip Not Found");

                var driver = _unitOfWork.DriverRepository.Get(d => d.DriverId == trip.DriverId);
                if (driver == null) return new BusinessResult(404, "Driver Not Found");

                var beingReport =  _unitOfWork.IncidentReportsRepository.Get(i => i.TripId == tripId && i.Status == "Handling");
                if (beingReport != null) return new BusinessResult(400, "Cannot update status as there is an incident report being handled");

                var currentStatus = await _unitOfWork.DeliveryStatusRepository.GetByIdAsync(trip.Status);
                var newStatus = await _unitOfWork.DeliveryStatusRepository.GetByIdAsync(newStatusId);
                if (newStatus == null) return new BusinessResult(404, "Status not existed");

                var higherSecondStatus = await _unitOfWork.DeliveryStatusRepository.GetSecondHighestStatusIndexAsync();

                if (currentStatus?.StatusIndex == higherSecondStatus.StatusIndex + 1)
                    return new BusinessResult(400, "Cannot update completed order");

                if (currentStatus != null &&
                    newStatus.StatusIndex != currentStatus.StatusIndex + 1 &&
                    newStatus.StatusId != "delaying" && newStatus.StatusId != "canceled")
                {
                    return new BusinessResult(400, "Cannot update as over step status");
                }

                if (newStatus.StatusIndex == 1)
                {
                    trip.StartTime = DateTime.Now;
                    await _unitOfWork.TripRepository.UpdateAsync(trip);
                    await UpdateOrderAndVehiclesAsync(trip, "Delivering", VehicleStatus.OnDuty, DriverStatus.OnDuty);
                }

                trip.Status = newStatusId;

                if (newStatus.StatusIndex == higherSecondStatus.StatusIndex + 1)
                {
                    trip.EndTime = DateTime.Now;
                    driver.TotalProcessedOrders++;
                    await UpdateOrderAndVehiclesAsync(trip, "Completed", VehicleStatus.Active, DriverStatus.Active);
                    await _unitOfWork.DriverRepository.UpdateAsync(driver);
                }

                await _unitOfWork.TripRepository.UpdateAsync(trip);

                await _unitOfWork.TripStatusHistoryRepository.CreateAsync(new TripStatusHistory
                {
                    HistoryId = Guid.NewGuid().ToString(),
                    TripId = tripId,
                    StatusId = newStatusId,
                    StartTime = DateTime.Now
                });

                await _unitOfWork.CommitTransactionAsync();
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(trip.OrderId);
                await _notificationService.SendNotificationAsync(order.CreatedBy, "Trip Status Update", $"Your trip status has been updated to {newStatus.StatusName}", driver.FullName);
                return new BusinessResult(200, "Update Trip Success");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, "Internal Server Error");
            }
        }

        private async Task UpdateOrderAndVehiclesAsync(Trip trip, string orderStatus, VehicleStatus vehicleStatus, DriverStatus driverStatus)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(trip.OrderId);
            if (order != null)
            {
                order.Status = orderStatus;
                await _unitOfWork.OrderRepository.UpdateAsync(order);
            }

            var trailer = await _unitOfWork.TrailerRepository.GetByIdAsync(trip.TrailerId);
            if (trailer != null)
            {
                trailer.Status = vehicleStatus.ToString();
                await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
            }

            var tractor = await _unitOfWork.TractorRepository.GetByIdAsync(trip.TractorId);
            if (tractor != null)
            {
                tractor.Status = vehicleStatus.ToString();
                await _unitOfWork.TractorRepository.UpdateAsync(tractor);
            }

            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(trip.DriverId);
            if (driver != null)
            {
                driver.Status = (int?)driverStatus;
                await _unitOfWork.DriverRepository.UpdateAsync(driver);
            }
        }
        #endregion 

        #region Update Trip need change vehicale
        public async Task<BusinessResult> UpdateTripAsync(string tripId, UpdateTripRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

                await _unitOfWork.BeginTransactionAsync();
                var oldTrip = await _unitOfWork.TripRepository.GetByIdAsync(tripId);
                if (oldTrip == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, "Trip không tồn tại");

                // Validate trailer if being changed
                if (!string.IsNullOrEmpty(model.TrailerId) && model.TrailerId != oldTrip.TrailerId)
                {
                    var oldTrailer = await _unitOfWork.TrailerRepository.GetByIdAsync(oldTrip.TrailerId);
                    var newTrailer = await _unitOfWork.TrailerRepository.GetByIdAsync(model.TrailerId);

                    // Check if new trailer exists
                    if (newTrailer == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Trailer mới không tồn tại");

                    // Check trailer status - must be Active
                    if (newTrailer.Status != VehicleStatus.Active.ToString())
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Trailer phải ở trạng thái Active để được chỉ định cho Trip");

                    // Check load weight compatibility
                    if (newTrailer.MaxLoadWeight < oldTrailer.MaxLoadWeight)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tải trọng của Trailer mới không phù hợp");
                }

                // Validate tractor if being changed
                if (!string.IsNullOrEmpty(model.TractorId) && model.TractorId != oldTrip.TractorId)
                {
                    var oldTractor = await _unitOfWork.TractorRepository.GetByIdAsync(oldTrip.TractorId);
                    var newTractor = await _unitOfWork.TractorRepository.GetByIdAsync(model.TractorId);

                    // Check if new tractor exists
                    if (newTractor == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tractor mới không tồn tại");

                    // Check tractor status - must be Active
                    if (newTractor.Status != VehicleStatus.Active.ToString())
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tractor phải ở trạng thái Active để được chỉ định cho Trip");

                    // Check load weight compatibility
                    if (newTractor.MaxLoadWeight < oldTractor.MaxLoadWeight)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tải trọng của Tractor mới không phù hợp");
                }

                // Validate driver if being changed
                if (!string.IsNullOrEmpty(model.DriverId) && model.DriverId != oldTrip.DriverId)
                {
                    var newDriver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(model.DriverId);

                    // Check if new driver exists
                    if (newDriver == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Driver mới không tồn tại");

                    // Check driver status - must be Active (1)
                    if (newDriver.Status != (int)DriverStatus.Active)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Driver phải ở trạng thái Active để được chỉ định cho Trip");
                }
                var previousStatus = await _unitOfWork.TripStatusHistoryRepository.GetPreviousStatusOfTrip(oldTrip.TripId);

                // Create new trip with updated values
                var newTrip = new Trip
                {
                    TripId = Guid.NewGuid().ToString(),
                    OrderId = oldTrip.OrderId,
                    DriverId = model.DriverId ?? oldTrip.DriverId,
                    TractorId = model.TractorId ?? oldTrip.TractorId,
                    TrailerId = model.TrailerId ?? oldTrip.TrailerId,
                    Status = previousStatus.StatusId,
                    StartTime = DateTime.Now,
                    MatchType = 2,
                    MatchBy = userName,
                    MatchTime = DateTime.Now,
                };

                // Create the new trip
                await _unitOfWork.TripRepository.CreateAsync(newTrip);

                // Update vehicle and driver status to OnDuty
                if (model.TrailerId != null && model.TrailerId != oldTrip.TrailerId)
                {
                    var trailer = await _unitOfWork.TrailerRepository.GetByIdAsync(model.TrailerId);
                    trailer.Status = VehicleStatus.OnDuty.ToString();
                    await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
                }

                if (model.TractorId != null && model.TractorId != oldTrip.TractorId)
                {
                    var tractor = await _unitOfWork.TractorRepository.GetByIdAsync(model.TractorId);
                    tractor.Status = VehicleStatus.OnDuty.ToString();
                    await _unitOfWork.TractorRepository.UpdateAsync(tractor);
                }

                if (model.DriverId != null && model.DriverId != oldTrip.DriverId)
                {
                    var driver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(model.DriverId);
                    driver.Status = (int)DriverStatus.OnDuty;
                    await _unitOfWork.DriverRepository.UpdateAsync(driver);
                }

                // Create trip status history entry
                await _unitOfWork.TripStatusHistoryRepository.CreateAsync(new TripStatusHistory
                {
                    HistoryId = Guid.NewGuid().ToString(),
                    TripId = newTrip.TripId,
                    StatusId = previousStatus.StatusId,
                    StartTime = DateTime.Now
                });

                await _unitOfWork.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công", newTrip);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.FAIL_UPDATE_CODE, ex.Message);
            }
        }
        #endregion

        #region Create Trip Manual
        public async Task<BusinessResult> CreateTripAsync(CreateTripRequestModel tripRequestModel, ClaimsPrincipal claims)
        {
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            // Lấy Order
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(tripRequestModel.OrderId);
            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng!");

            if (!order.DeliveryDate.HasValue)
                throw new Exception("Đơn hàng không có ngày giao hàng (DeliveryDate)!");

            var deliveryDate = order.DeliveryDate.Value;

            // Kiểm tra Tractor
            var tractor = await _unitOfWork.TractorRepository.GetByIdAsync(tripRequestModel.TractorId);
            if (tractor == null || (tractor.Status != "OnDuty" && tractor.Status != "Active"))
                throw new Exception("Tractor không khả dụng!");

            // Kiểm tra Trailer
            var trailer = await _unitOfWork.TrailerRepository.GetByIdAsync(tripRequestModel.TrailerId);
            if (trailer == null || (trailer.Status != "OnDuty" && trailer.Status != "Active"))
                throw new Exception("Trailer không khả dụng!");

            // Kiểm tra Driver
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(tripRequestModel.DriverId);
            if (driver == null || (driver.Status != 1 && driver.Status != 2))
                throw new Exception("Tài xế không khả dụng!");

            // Tính trọng lượng container (kg)
            double containerWeight = 0;
            int? configId = order.ContainerType switch
            {
                1 when order.ContainerSize == 20 => 1,
                1 when order.ContainerSize == 40 => 2,
                2 when order.ContainerSize == 20 => 3,
                2 when order.ContainerSize == 40 => 4,
                _ => null
            };

            if (configId.HasValue)
            {
                var config = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(configId.Value);
                if (config == null || !double.TryParse(config.ConfigValue, out containerWeight))
                    throw new Exception("Không tìm thấy dữ liệu trọng lượng container!");
            }

            double totalWeight = (double)(order.Weight ?? 0) + (containerWeight / 1000.0);

            if ((double)(trailer.MaxLoadWeight ?? 0) < totalWeight)
                throw new Exception("Tải trọng của rơ-moóc không đủ!");

            if (!tractor.MaxLoadWeight.HasValue || tractor.MaxLoadWeight.Value < (decimal)totalWeight)
                throw new Exception("Tải trọng vượt quá khả năng kéo của đầu kéo!");

            // Tính thời gian hoàn thành đơn hàng (phút)
            var completionTimeSpan = order.CompletionTime?.ToTimeSpan() ?? TimeSpan.Zero;
            int completionMinutes = (int)completionTimeSpan.TotalMinutes;

            // Lấy cấu hình thời gian giới hạn ngày & tuần
            var dailyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(5);
            var weeklyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(6);

            if (dailyLimitConfig == null || weeklyLimitConfig == null)
                throw new Exception("Không tìm thấy cấu hình giới hạn thời gian!");

            if (!int.TryParse(dailyLimitConfig.ConfigValue, out int dailyHourLimit))
                throw new Exception("Cấu hình giới hạn thời gian ngày không hợp lệ!");

            if (!int.TryParse(weeklyLimitConfig.ConfigValue, out int weeklyHourLimit))
                throw new Exception("Cấu hình giới hạn thời gian tuần không hợp lệ!");

            // Kiểm tra giới hạn theo ngày
            var dailyRecord = await _unitOfWork.DriverDailyWorkingTimeRepository
                .GetByDriverIdAndDateAsync(driver.DriverId, deliveryDate);

            int totalDailyTime = (dailyRecord?.TotalTime ?? 0) + completionMinutes;
            if (totalDailyTime > dailyHourLimit * 60)
                throw new Exception($"Tài xế đã vượt quá thời gian làm việc trong ngày ({dailyHourLimit} giờ)!");

            // Tính tuần: từ chủ nhật -> thứ bảy
            var deliveryDateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
            var weekStart = DateOnly.FromDateTime(deliveryDateTime.AddDays(-(int)deliveryDateTime.DayOfWeek));
            var weekEnd = weekStart.AddDays(6);

            var weeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                .GetByDriverIdAndWeekAsync(driver.DriverId, weekStart, weekEnd);

            int totalWeeklyTime = (weeklyRecord?.TotalHours ?? 0) + completionMinutes;
            if (totalWeeklyTime > weeklyHourLimit * 60)
                throw new Exception($"Tài xế đã vượt quá thời gian làm việc trong tuần ({weeklyHourLimit} giờ)!");

            // Tạo Trip
            var trip = new Trip
            {
                TripId = Guid.NewGuid().ToString(),
                OrderId = tripRequestModel.OrderId,
                DriverId = tripRequestModel.DriverId,
                TractorId = tripRequestModel.TractorId,
                TrailerId = tripRequestModel.TrailerId,
                Status = "not_started",
                MatchType = 1,
                MatchBy = userName,
                MatchTime = DateTime.Now
            };

            try
            {
                await _unitOfWork.TripRepository.CreateAsync(trip);

                // Cập nhật trạng thái đơn hàng
                order.Status = "scheduled";
                await _unitOfWork.OrderRepository.UpdateAsync(order);

                // Cập nhật thời gian làm việc ngày
                if (dailyRecord != null)
                {
                    dailyRecord.TotalTime += completionMinutes;
                    dailyRecord.ModifiedDate = DateTime.Now;
                    dailyRecord.ModifiedBy = userName;
                    await _unitOfWork.DriverDailyWorkingTimeRepository.UpdateAsync(dailyRecord);
                }
                else
                {
                    var newDaily = new DriverDailyWorkingTime
                    {
                        RecordId = Guid.NewGuid().ToString(),
                        DriverId = driver.DriverId,
                        WorkDate = deliveryDate,
                        TotalTime = completionMinutes,
                        CreatedBy = userName,
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = userName
                    };
                    await _unitOfWork.DriverDailyWorkingTimeRepository.CreateAsync(newDaily);
                }

                // Cập nhật thời gian làm việc tuần
                if (weeklyRecord != null)
                {
                    weeklyRecord.TotalHours = (weeklyRecord.TotalHours ?? 0) + completionMinutes;
                    await _unitOfWork.DriverWeeklySummaryRepository.UpdateAsync(weeklyRecord);
                }
                else
                {
                    var newWeekly = new DriverWeeklySummary
                    {
                        SummaryId = Guid.NewGuid().ToString(),
                        DriverId = driver.DriverId,
                        WeekStart = weekStart,
                        WeekEnd = weekEnd,
                        TotalHours = completionMinutes,
                    };
                    await _unitOfWork.DriverWeeklySummaryRepository.CreateAsync(newWeekly);
                }

                return new BusinessResult(1, "Tạo trip thành công!", trip);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return new BusinessResult(-1, $"Lỗi khi tạo trip: {errorMessage}");
            }
        }
        #endregion
    }
}

