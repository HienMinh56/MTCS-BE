using System.Globalization;
using System.Security.Claims;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.DTOs.TripsDTO;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class TripService : ITripService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public TripService(UnitOfWork unitOfWork, INotificationService notificationService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        #region GetTripsByFilter
        public async Task<BusinessResult> GetTripsByFilterAsync(
            string? tripId,
            string? driverId,
            string? status,
            string? tractorId,
            string? trailerId,
            string? orderId,
            string? trackingCode,
            string? tractorlicensePlate,
            string? trailerlicensePlate)
        {
            try
            {
                var trips = await _unitOfWork.TripRepository.GetTripsByFilterAsync(
                    tripId, driverId, status, tractorId, trailerId, orderId, trackingCode, tractorlicensePlate, trailerlicensePlate
                );

                if (trips == null)
                {
                    return new BusinessResult(404, "No trips found.");
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

                var trip = _unitOfWork.TripRepository.Get(t => t.TripId == tripId);
                if (trip == null) return new BusinessResult(404, "Trip Not Found");

                var driver = _unitOfWork.DriverRepository.Get(d => d.DriverId == trip.DriverId);
                if (driver == null) return new BusinessResult(404, "Driver Not Found");

                var beingReport = _unitOfWork.IncidentReportsRepository.Get(i => i.TripId == tripId && i.Status == "Handling");
                if (beingReport != null) return new BusinessResult(400, "Cannot update status as there is an incident report being handled");

                var currentStatus = await _unitOfWork.DeliveryStatusRepository.GetByIdAsync(trip.Status);
                var newStatus = await _unitOfWork.DeliveryStatusRepository.GetByIdAsync(newStatusId);
                if (newStatus == null) return new BusinessResult(404, "Status not existed");

                if (currentStatus?.StatusId == "completed")
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

                    var orderDetail = await _unitOfWork.OrderDetailRepository.GetByIdAsync(trip.OrderDetailId);
                    var isAnyDelivering = await _unitOfWork.OrderDetailRepository.AnyOrderDetailDeliveringAsync(orderDetail.OrderId);
                    var updateOrderStatus = isAnyDelivering ? null : "Delivering";

                    await UpdateOrderAndVehiclesAsync(
                        trip,
                        updateOrderStatus,
                        VehicleStatus.OnDuty,
                        DriverStatus.OnDuty,
                        orderDetailStatus: "Delivering"
                    );
                }

                trip.Status = newStatusId;

                if (newStatus.StatusId == "completed")
                {
                    trip.EndTime = DateTime.Now;
                    driver.TotalProcessedOrders++;

                    var orderDetail = await _unitOfWork.OrderDetailRepository.GetOrderDetailWithTripsAsync(trip.OrderDetailId);

                    var driverStatus = DriverStatus.OnDuty;
                    if (!await _unitOfWork.TripRepository.IsDriverHaveProcessTrip(trip.DriverId, trip.TripId))
                    {
                        driverStatus = DriverStatus.Active;
                    }

                    await UpdateOrderAndVehiclesAsync(
                        trip,
                        null,
                        VehicleStatus.Active,
                        driverStatus,
                        orderDetailStatus: "Completed"
                    );

                    var allCompleted = await _unitOfWork.OrderDetailRepository
                        .AreAllOrderDetailsCompletedAsync(orderDetail.OrderId);

                    if (allCompleted)
                    {
                        var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderDetail.OrderId);
                        order.Status = "Completed";
                        await _unitOfWork.OrderRepository.UpdateAsync(order);
                    }

                    await _notificationService.SendNotificationAsync(
                        orderDetail.Order.CreatedBy,
                        "Chuyến đi đã được cập nhật",
                        $"Chuyến {tripId} đã được cập nhật thành '{newStatus.StatusName}' bởi {driver.FullName} vào lúc {trip.EndTime}",
                        driver.FullName
                    );

                    string subject = "Đơn hàng của bạn đã được giao thành công";
                    await _emailService.SendOrderCompletionToCustomer(
                        orderDetail.Order.Customer.Email,
                        subject,
                        orderDetail.Order.Customer.CompanyName,
                        orderDetail.Order.TrackingCode,
                        orderDetail.CompletionTime
                    );

                    if (trip.StartTime.HasValue && trip.EndTime.HasValue && orderDetail.CompletionTime.HasValue)
                    {
                        var actualMinutes = (int)(trip.EndTime.Value - trip.StartTime.Value).TotalMinutes;
                        var estimatedMinutes = (int)orderDetail.CompletionTime.Value.ToTimeSpan().TotalMinutes;
                        var diffMinutes = actualMinutes - estimatedMinutes; // chênh lệch thực tế so với ước lượng

                        var workDate = DateOnly.FromDateTime(trip.EndTime.Value);
                        var culture = System.Globalization.CultureInfo.CurrentCulture;
                        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
                        var weekStart = trip.EndTime.Value.Date.AddDays(-(int)(trip.EndTime.Value.DayOfWeek - firstDayOfWeek + 7) % 7);
                        var weekEnd = weekStart.AddDays(6);

                        var daily = _unitOfWork.DriverDailyWorkingTimeRepository
                            .Get(d => d.DriverId == driver.DriverId && d.WorkDate == workDate);

                        var weekly = _unitOfWork.DriverWeeklySummaryRepository
                            .Get(w => w.DriverId == driver.DriverId && w.WeekStart == DateOnly.FromDateTime(weekStart) && w.WeekEnd == DateOnly.FromDateTime(weekEnd));

                        if (daily != null)
                        {
                            daily.TotalTime = Math.Max(0, (daily.TotalTime ?? 0) + diffMinutes);
                            daily.ModifiedDate = DateTime.Now;
                            daily.ModifiedBy = userId;
                            await _unitOfWork.DriverDailyWorkingTimeRepository.UpdateAsync(daily);
                        }

                        if (weekly != null)
                        {
                            // Weekly tính theo giờ nên chuyển từ phút sang giờ chính xác hơn bằng double
                            var currentHours = weekly.TotalHours ?? 0;
                            weekly.TotalHours = Math.Max(0, currentHours + diffMinutes);
                            await _unitOfWork.DriverWeeklySummaryRepository.UpdateAsync(weekly);
                        }
                    }


                    // --- Kết thúc phần cập nhật thời gian làm việc ---
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

                return new BusinessResult(200, "Update Trip Success");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, "Internal Server Error");
            }
        }





        private async Task UpdateOrderAndVehiclesAsync(
            Trip trip,
            string? orderStatus,
            VehicleStatus vehicleStatus,
            DriverStatus driverStatus,
            string? orderDetailStatus = null)
        {
            var orderDetail = await _unitOfWork.OrderDetailRepository.GetByIdAsync(trip.OrderDetailId);

            if (!string.IsNullOrEmpty(orderDetailStatus))
            {
                orderDetail.Status = orderDetailStatus;
                await _unitOfWork.OrderDetailRepository.UpdateAsync(orderDetail);
            }

            if (!string.IsNullOrEmpty(orderStatus))
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderDetail.OrderId);
                if (order != null)
                {
                    order.Status = orderStatus;
                    await _unitOfWork.OrderRepository.UpdateAsync(order);
                }
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

                //Get order to retrieve completion time and delivery date
                var orderDetail = await _unitOfWork.OrderDetailRepository.GetByIdAsync(oldTrip.OrderDetailId);
                if (orderDetail == null || !orderDetail.DeliveryDate.HasValue)
                    return new BusinessResult(Const.FAIL_READ_CODE, "Không tìm thấy đơn hàng hoặc đơn hàng không có ngày giao hàng!");

                var deliveryDate = orderDetail.DeliveryDate.Value;

                //Calculate completion time in minutes
                var completionTimeSpan = orderDetail.CompletionTime?.ToTimeSpan() ?? TimeSpan.Zero;
                int completionMinutes = (int)completionTimeSpan.TotalMinutes;

                //Validate trailer if being changed
                if (!string.IsNullOrEmpty(model.TrailerId) && model.TrailerId != oldTrip.TrailerId)
                {
                    var oldTrailer = await _unitOfWork.TrailerRepository.GetByIdAsync(oldTrip.TrailerId);
                    var newTrailer = await _unitOfWork.TrailerRepository.GetByIdAsync(model.TrailerId);

                    //Check if new trailer exists
                    if (newTrailer == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Trailer mới không tồn tại");

                    //Check trailer status - must be Active
                    if (newTrailer.Status != VehicleStatus.Active.ToString())
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Trailer phải ở trạng thái Active để được chỉ định cho Trip");

                    //Check load weight compatibility
                    if (newTrailer.MaxLoadWeight < oldTrailer.MaxLoadWeight)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tải trọng của Trailer mới không phù hợp");
                }

                //Validate tractor if being changed
                if (!string.IsNullOrEmpty(model.TractorId) && model.TractorId != oldTrip.TractorId)
                {
                    var oldTractor = await _unitOfWork.TractorRepository.GetByIdAsync(oldTrip.TractorId);
                    var newTractor = await _unitOfWork.TractorRepository.GetByIdAsync(model.TractorId);

                    //Check if new tractor exists
                    if (newTractor == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tractor mới không tồn tại");

                    //Check tractor status - must be Active
                    if (newTractor.Status != VehicleStatus.Active.ToString())
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tractor phải ở trạng thái Active để được chỉ định cho Trip");

                    //Check load weight compatibility
                    if (newTractor.MaxLoadWeight < orderDetail.Weight)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tải trọng của Tractor mới không phù hợp");
                }

                //Validate driver if being changed
                string oldDriverId = oldTrip.DriverId;
                string newDriverId = model.DriverId ?? oldDriverId;
                bool isDriverChanged = !string.IsNullOrEmpty(model.DriverId) && model.DriverId != oldDriverId;

                if (isDriverChanged)
                {
                    var newDriver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(model.DriverId);

                    //Check if new driver exists
                    if (newDriver == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Driver mới không tồn tại");

                    //Check driver status - must be Active(1)
                    if (newDriver.Status != (int)DriverStatus.Active)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Driver phải ở trạng thái Active để được chỉ định cho Trip");

                    //Get driver working hour limits
                    var dailyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(5);
                    var weeklyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(6);

                    if (dailyLimitConfig == null || weeklyLimitConfig == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Không tìm thấy cấu hình giới hạn thời gian!");

                    if (!int.TryParse(dailyLimitConfig.ConfigValue, out int dailyHourLimit))
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Cấu hình giới hạn thời gian ngày không hợp lệ!");

                    if (!int.TryParse(weeklyLimitConfig.ConfigValue, out int weeklyHourLimit))
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Cấu hình giới hạn thời gian tuần không hợp lệ!");

                    //Check daily working time for new driver

                    var dailyRecord = await _unitOfWork.DriverDailyWorkingTimeRepository
                        .GetByDriverIdAndDateAsync(newDriverId, deliveryDate);

                    int totalDailyTime = (dailyRecord?.TotalTime ?? 0) + completionMinutes;
                    if (totalDailyTime > dailyHourLimit * 60)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, $"Tài xế đã vượt quá thời gian làm việc trong ngày ({dailyHourLimit} giờ)!");

                    //Calculate week boundaries
                    var deliveryDateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
                    var weekStart = DateOnly.FromDateTime(deliveryDateTime.AddDays(-(int)deliveryDateTime.DayOfWeek));
                    var weekEnd = weekStart.AddDays(6);

                    //Check weekly working time for new driver

                    var weeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                        .GetByDriverIdAndWeekAsync(newDriverId, weekStart, weekEnd);

                    int totalWeeklyTime = (weeklyRecord?.TotalHours ?? 0) + completionMinutes;
                    if (totalWeeklyTime > weeklyHourLimit * 60)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, $"Tài xế đã vượt quá thời gian làm việc trong tuần ({weeklyHourLimit} giờ)!");
                }

                var previousStatus = await _unitOfWork.TripStatusHistoryRepository.GetPreviousStatusOfTrip(oldTrip.TripId);

                //Create new trip with updated values
                var newTrip = new Trip
                {
                    TripId = "TRIP" + Guid.NewGuid().ToString("N").Substring(0, 10),
                    OrderDetailId = oldTrip.OrderDetailId,
                    DriverId = newDriverId,
                    TractorId = model.TractorId ?? oldTrip.TractorId,
                    TrailerId = model.TrailerId ?? oldTrip.TrailerId,
                    Status = previousStatus.StatusId,
                    StartTime = DateTime.Now,
                    MatchType = 2,
                    MatchBy = userName,
                    MatchTime = DateTime.Now,
                };

                //Create the new trip
                await _unitOfWork.TripRepository.CreateAsync(newTrip);

                //Update vehicle and driver status to OnDuty
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

                if (isDriverChanged)
                {
                    //Update driver status
                    var driver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(newDriverId);
                    driver.Status = (int)DriverStatus.OnDuty;
                    await _unitOfWork.DriverRepository.UpdateAsync(driver);

                    //Calculate week boundaries for working time

                    var deliveryDateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
                    var weekStart = DateOnly.FromDateTime(deliveryDateTime.AddDays(-(int)deliveryDateTime.DayOfWeek));
                    var weekEnd = weekStart.AddDays(6);

                    //Update or create daily working time record for new driver

                    var dailyRecord = await _unitOfWork.DriverDailyWorkingTimeRepository
                        .GetByDriverIdAndDateAsync(newDriverId, deliveryDate);

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
                            DriverId = newDriverId,
                            WorkDate = deliveryDate,
                            TotalTime = completionMinutes,
                            CreatedBy = userName,
                            ModifiedDate = DateTime.Now,
                            ModifiedBy = userName
                        };
                        await _unitOfWork.DriverDailyWorkingTimeRepository.CreateAsync(newDaily);
                    }

                    //Update or create weekly working time record for new driver

                    var weeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                        .GetByDriverIdAndWeekAsync(newDriverId, weekStart, weekEnd);

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
                            DriverId = newDriverId,
                            WeekStart = weekStart,
                            WeekEnd = weekEnd,
                            TotalHours = completionMinutes,
                        };
                        await _unitOfWork.DriverWeeklySummaryRepository.CreateAsync(newWeekly);
                    }

                    //Remove hours from old driver's records if needed
                    if (oldDriverId != newDriverId)
                    {
                        //Update old driver's daily record
                        var oldDailyRecord = await _unitOfWork.DriverDailyWorkingTimeRepository
                            .GetByDriverIdAndDateAsync(oldDriverId, deliveryDate);

                        if (oldDailyRecord != null)
                        {
                            oldDailyRecord.TotalTime = oldDailyRecord.TotalTime.HasValue
                                                       ? Math.Max(0, oldDailyRecord.TotalTime.Value - completionMinutes)
                                                       : 0;
                            oldDailyRecord.ModifiedDate = DateTime.Now;
                            oldDailyRecord.ModifiedBy = userName;
                            await _unitOfWork.DriverDailyWorkingTimeRepository.UpdateAsync(oldDailyRecord);
                        }

                        //Update old driver's weekly record
                        var oldWeeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                            .GetByDriverIdAndWeekAsync(oldDriverId, weekStart, weekEnd);

                        if (oldWeeklyRecord != null)
                        {
                            oldWeeklyRecord.TotalHours = Math.Max(0, (oldWeeklyRecord.TotalHours ?? 0) - completionMinutes);
                            await _unitOfWork.DriverWeeklySummaryRepository.UpdateAsync(oldWeeklyRecord);
                        }
                    }
                }

                //Create trip status history entry
                await _unitOfWork.TripStatusHistoryRepository.CreateAsync(new TripStatusHistory
                {
                    HistoryId = Guid.NewGuid().ToString(),
                    TripId = newTrip.TripId,
                    StatusId = previousStatus.StatusId,
                    StartTime = DateTime.Now
                });

                await _unitOfWork.CommitTransactionAsync();

                var incident = await _unitOfWork.IncidentReportsRepository.GetRecentIncidentReport(tripId);
                await _notificationService.SendNotificationAsync(newTrip.DriverId, "Bạn vừa nhận được 1 chuyến hàng mới", $"Chuyến {newTrip.TripId} bắt đầu từ {incident.Location} để thay thế chuyến {tripId}.", "Hệ thống");

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

            //Lấy OrderDetail
            var orderDetail = await _unitOfWork.OrderDetailRepository.GetByIdAsync(tripRequestModel.OrderDetailId);
            if (orderDetail == null)
                throw new Exception("Không tìm thấy đơn hàng!");

            if (!orderDetail.DeliveryDate.HasValue)
                throw new Exception("Đơn hàng không có ngày giao hàng (DeliveryDate)!");

            var deliveryDate = orderDetail.DeliveryDate.Value;

            //Kiểm tra Tractor
            var tractor = await _unitOfWork.TractorRepository.GetByIdAsync(tripRequestModel.TractorId);
            if (tractor == null || (tractor.Status != "OnDuty" && tractor.Status != "Active"))
                throw new Exception("Tractor không khả dụng!");

            // Kiểm tra Trailer
            var trailer = await _unitOfWork.TrailerRepository.GetByIdAsync(tripRequestModel.TrailerId);
            if (trailer == null || (trailer.Status != "OnDuty" && trailer.Status != "Active"))
                throw new Exception("Trailer không khả dụng!");

            //Kiểm tra Driver
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(tripRequestModel.DriverId);
            if (driver == null || (driver.Status != 1 && driver.Status != 2))
                throw new Exception("Tài xế không khả dụng!");

            //Kiểm tra ContainerType của Order và Tractor
            if (orderDetail.ContainerType == 2) // Order lạnh
            {
                //Tractor phải là lạnh(2)
                if (tractor.ContainerType != 2)
                {
                    throw new Exception("Tractor không phù hợp với loại container lạnh của đơn hàng!");
                }
            }

            //Tính trọng lượng container(kg)
            double containerWeight = 0;
            int? configId = orderDetail.ContainerType switch
            {
                1 when orderDetail.ContainerSize == 20 => 1,
                1 when orderDetail.ContainerSize == 40 => 2,
                2 when orderDetail.ContainerSize == 20 => 3,
                2 when orderDetail.ContainerSize == 40 => 4,
                _ => null
            };

            if (configId.HasValue)
            {
                var config = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(configId.Value);
                if (config == null || !double.TryParse(config.ConfigValue, out containerWeight))
                    throw new Exception("Không tìm thấy dữ liệu trọng lượng container!");
            }

            double totalWeight = (double)(orderDetail.Weight ?? 0) + (containerWeight / 1000.0);

            if ((double)(trailer.MaxLoadWeight ?? 0) < totalWeight)
                throw new Exception("Tải trọng của rơ-moóc không đủ!");

            if (!tractor.MaxLoadWeight.HasValue || tractor.MaxLoadWeight.Value < (decimal)totalWeight)
                throw new Exception("Tải trọng vượt quá khả năng kéo của đầu kéo!");

            //Tính thời gian hoàn thành đơn hàng(phút)
            var completionTimeSpan = orderDetail.CompletionTime?.ToTimeSpan() ?? TimeSpan.Zero;
            int completionMinutes = (int)completionTimeSpan.TotalMinutes;

            //Lấy cấu hình thời gian giới hạn ngày &tuần
            var dailyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(5);
            var weeklyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(6);

            if (dailyLimitConfig == null || weeklyLimitConfig == null)
                throw new Exception("Không tìm thấy cấu hình giới hạn thời gian!");

            if (!int.TryParse(dailyLimitConfig.ConfigValue, out int dailyHourLimit))
                throw new Exception("Cấu hình giới hạn thời gian ngày không hợp lệ!");

            if (!int.TryParse(weeklyLimitConfig.ConfigValue, out int weeklyHourLimit))
                throw new Exception("Cấu hình giới hạn thời gian tuần không hợp lệ!");

            //Kiểm tra giới hạn theo ngày
            var dailyRecord = await _unitOfWork.DriverDailyWorkingTimeRepository
                .GetByDriverIdAndDateAsync(driver.DriverId, deliveryDate);

            int totalDailyTime = (dailyRecord?.TotalTime ?? 0) + completionMinutes;
            if (totalDailyTime > dailyHourLimit * 60)
                throw new Exception($"Tài xế đã vượt quá thời gian làm việc trong ngày ({dailyHourLimit} giờ)!");

            //Tính tuần: từ chủ nhật->thứ bảy
            var deliveryDateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
            var weekStart = DateOnly.FromDateTime(deliveryDateTime.AddDays(-(int)deliveryDateTime.DayOfWeek));
            var weekEnd = weekStart.AddDays(6);

            var weeklyRecord = await _unitOfWork.DriverWeeklySummaryRepository
                .GetByDriverIdAndWeekAsync(driver.DriverId, weekStart, weekEnd);

            int totalWeeklyTime = (weeklyRecord?.TotalHours ?? 0) + completionMinutes;
            if (totalWeeklyTime > weeklyHourLimit * 60)
                throw new Exception($"Tài xế đã vượt quá thời gian làm việc trong tuần ({weeklyHourLimit} giờ)!");

            //Tạo Trip
            var trip = new Trip
            {
                TripId = "TRIP" + Guid.NewGuid().ToString("N").Substring(0, 10),
                OrderDetailId = tripRequestModel.OrderDetailId,
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

                //Cập nhật trạng thái đơn hàng
                orderDetail.Status = "Scheduled";
                await _unitOfWork.OrderDetailRepository.UpdateAsync(orderDetail);

                //Cập nhật thời gian làm việc ngày
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

                //Cập nhật thời gian làm việc tuần
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
                var existingOrder = await _unitOfWork.OrderDetailRepository.GetByIdAsync(trip.OrderDetailId);
                //Gửi thông báo sau khi cập nhật thành công
                await _notificationService.SendNotificationAsync(trip.DriverId, "Bạn vừa nhận được 1 chuyến hàng mới", $"Chuyến {trip.TripId} được xếp bởi {trip.MatchBy} xuất phát từ {existingOrder.PickUpLocation}.", "Hệ thống");

                return new BusinessResult(1, "Tạo trip thành công!", trip);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return new BusinessResult(-1, $"Lỗi khi tạo trip: {errorMessage}");
            }
        }
        #endregion

        #region CancelTrip
        public async Task<IBusinessResult> CancelTrip(CancelTripRequest request, ClaimsPrincipal claims)
        {
            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

            var trip = _unitOfWork.TripRepository.Get(t => t.TripId == request.TripId);
            var orderDetail = _unitOfWork.OrderDetailRepository.Get(o => o.OrderDetailId == trip.OrderDetailId);

            if (trip == null)
                return new BusinessResult(Const.FAIL_READ_CODE, "Trip không tồn tại");
            orderDetail.Status = "Pending";
            trip.Note = request.Note;
            trip.Status = "canceled";
            trip.StartTime = DateTime.Now;
            trip.EndTime = DateTime.Now;
            await _unitOfWork.OrderDetailRepository.UpdateAsync(orderDetail);
            await _unitOfWork.TripRepository.UpdateAsync(trip);

            await _unitOfWork.TripStatusHistoryRepository.CreateAsync(new TripStatusHistory
            {
                HistoryId = Guid.NewGuid().ToString(),
                TripId = trip.TripId,
                StatusId = trip.Status,
                StartTime = DateTime.Now
            });

            await _notificationService.SendNotificationAsync(trip.DriverId, "Chuyến đi đã bị hủy", $"Chuyến {request.TripId} đã được cập nhật thành '{trip.Status}' lý do {request.Note} vào lúc {trip.EndTime} bởi {userName}.", userName);
            return new BusinessResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công", trip);
        }
        #endregion

        #region Create trip auto
        public async Task<BusinessResult> AutoScheduleTripsForOrderAsync(string orderDetailId)
        {
            var orderDetail = await _unitOfWork.OrderDetailRepository.GetByIdAsync(orderDetailId);
            if (orderDetail == null) return new BusinessResult(-1, "Không tìm thấy đơn hàng!");
            if (!orderDetail.DeliveryDate.HasValue) return new BusinessResult(-1, "Đơn hàng không có ngày giao hàng!");

            var deliveryDate = orderDetail.DeliveryDate.Value;

            //Tính trọng lượng
            double containerWeight = 0;
            int? configId = orderDetail.ContainerType switch
            {
                1 when orderDetail.ContainerSize == 20 => 1,
                1 when orderDetail.ContainerSize == 40 => 2,
                2 when orderDetail.ContainerSize == 20 => 3,
                2 when orderDetail.ContainerSize == 40 => 4,
                _ => null
            };

            if (configId.HasValue)
            {
                var config = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(configId.Value);
                if (config == null || !double.TryParse(config.ConfigValue, out containerWeight))
                    return new BusinessResult(-1, "Không tìm thấy trọng lượng container!");
            }

            decimal totalWeight = (orderDetail.Weight ?? 0) + ((decimal)containerWeight / 1000);

            var drivers = await _unitOfWork.DriverRepository.GetActiveDriversAsync();
            var tractors = await _unitOfWork.TractorRepository.GetActiveTractorsAsync();
            var trailers = await _unitOfWork.TrailerRepository.GetActiveTrailersAsync();

            var dailyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(5);
            var weeklyLimitConfig = await _unitOfWork.SystemConfigurationRepository.GetByIdAsync(6);
            if (dailyLimitConfig == null || weeklyLimitConfig == null)
                return new BusinessResult(-1, "Không tìm thấy cấu hình giới hạn thời gian!");

            if (!int.TryParse(dailyLimitConfig.ConfigValue, out int dailyHourLimit) ||
                !int.TryParse(weeklyLimitConfig.ConfigValue, out int weeklyHourLimit))
                return new BusinessResult(-1, "Giá trị giới hạn thời gian không hợp lệ!");

            var completionMinutes = (int)(orderDetail.CompletionTime?.ToTimeSpan().TotalMinutes ?? 0);

            //Lấy danh sách trips cùng ngày
            var tripsInDate = await _unitOfWork.TripRepository.GetByDateAsync(deliveryDate);

            var usedTractorIds = tripsInDate.Select(t => t.TractorId).ToHashSet();
            var usedTrailerIds = tripsInDate.Select(t => t.TrailerId).ToHashSet();

            //Ưu tiên đầu kéo khô nếu đơn hàng khô
            var preferredTractors = orderDetail.ContainerType == 1
                ? tractors.Where(t => t.ContainerType == 1).Concat(tractors.Where(t => t.ContainerType != 1))
                : tractors.Where(t => t.ContainerType == orderDetail.ContainerType);

            //foreach (var driver in drivers)
            //{
            //    //Check daily working time
            //    var daily = await _unitOfWork.DriverDailyWorkingTimeRepository.GetByDriverIdAndDateAsync(driver.DriverId, deliveryDate);
            //    int totalDaily = (daily?.TotalTime ?? 0) + completionMinutes;
            //    if (totalDaily > dailyHourLimit * 60) continue;

            //    //Check weekly working time
            //    var dateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
            //    var weekStart = DateOnly.FromDateTime(dateTime.AddDays(-(int)dateTime.DayOfWeek));
            //    var weekEnd = weekStart.AddDays(6);

            //    var weekly = await _unitOfWork.DriverWeeklySummaryRepository.GetByDriverIdAndWeekAsync(driver.DriverId, weekStart, weekEnd);
            //    int totalWeekly = (weekly?.TotalHours ?? 0) + completionMinutes;
            //    if (totalWeekly > weeklyHourLimit * 60) continue;

            //    //Check if driver already has trips today
            //    var driverTripsToday = await _unitOfWork.TripRepository.GetByDriverIdAndDateAsync(driver.DriverId, deliveryDate);

            //    if (driverTripsToday.Any())
            //    {
            //        var existingTractorId = driverTripsToday.First().TractorId;
            //        var existingTrailerId = driverTripsToday.First().TrailerId;

            //        var tractor = tractors.FirstOrDefault(t =>
            //            t.TractorId == existingTractorId &&
            //            t.MaxLoadWeight >= totalWeight &&
            //            t.RegistrationExpirationDate > deliveryDate);

            //        var trailer = trailers.FirstOrDefault(t =>
            //            t.TrailerId == existingTrailerId &&
            //            t.MaxLoadWeight >= totalWeight &&
            //            (orderDetail.ContainerSize != 40 || t.ContainerSize == 2) &&
            //            t.RegistrationExpirationDate > deliveryDate);

            //        if (tractor != null && trailer != null)
            //        {
            //            return await CreateTrip(orderDetail, driver, tractor, trailer, deliveryDate, completionMinutes, daily, weekly, weekStart, weekEnd);
            //        }

            //        continue; // Thiết bị không còn phù hợp
            //    }

            //    //Nếu chưa có trip, tìm thiết bị chưa ai dùng hôm đó
            //    foreach (var tractor in preferredTractors)
            //    {
            //        if (!tractor.MaxLoadWeight.HasValue ||
            //            tractor.MaxLoadWeight.Value < totalWeight ||
            //            usedTractorIds.Contains(tractor.TractorId) ||
            //            tractor.RegistrationExpirationDate <= deliveryDate)
            //            continue;

            //        foreach (var trailer in trailers.Where(t =>
            //            t.MaxLoadWeight >= totalWeight &&
            //            (orderDetail.ContainerSize != 40 || t.ContainerSize == 2) &&
            //            !usedTrailerIds.Contains(t.TrailerId) &&
            //            t.RegistrationExpirationDate > deliveryDate))
            //        {
            //            //Gán trip
            //            usedTractorIds.Add(tractor.TractorId);
            //            usedTrailerIds.Add(trailer.TrailerId);

            //            return await CreateTrip(orderDetail, driver, tractor, trailer, deliveryDate, completionMinutes, daily, weekly, weekStart, weekEnd);
            //        }
            //    }
            //}

            var driverWorkInfos = new List<(Driver driver, int totalDaily, int totalWeekly, DriverDailyWorkingTime? daily, DriverWeeklySummary? weekly)>();

            var dateTime = deliveryDate.ToDateTime(TimeOnly.MinValue);
            var weekStart = DateOnly.FromDateTime(dateTime.AddDays(-(int)dateTime.DayOfWeek));
            var weekEnd = weekStart.AddDays(6);

            // Tính và gom dữ liệu thời gian trước
            foreach (var driver in drivers)
            {
                var daily = await _unitOfWork.DriverDailyWorkingTimeRepository.GetByDriverIdAndDateAsync(driver.DriverId, deliveryDate);
                var weekly = await _unitOfWork.DriverWeeklySummaryRepository.GetByDriverIdAndWeekAsync(driver.DriverId, weekStart, weekEnd);

                int totalDaily = (daily?.TotalTime ?? 0) + completionMinutes;
                int totalWeekly = (weekly?.TotalHours ?? 0) + completionMinutes;

                if (totalDaily > dailyHourLimit * 60 || totalWeekly > weeklyHourLimit * 60)
                    continue;

                driverWorkInfos.Add((driver, totalDaily, totalWeekly, daily, weekly));
            }

            // Sắp xếp theo thời gian làm việc ít nhất
            var sortedDrivers = driverWorkInfos
                .OrderBy(info => info.totalDaily)
                .ThenBy(info => info.totalWeekly)
                .ToList();

            // Bắt đầu xét driver sau khi đã sort
            foreach (var (driver, totalDaily, totalWeekly, daily, weekly) in sortedDrivers)
            {
                var driverTripsToday = await _unitOfWork.TripRepository.GetByDriverIdAndDateAsync(driver.DriverId, deliveryDate);

                if (driverTripsToday.Any())
                {
                    var existingTractorId = driverTripsToday.First().TractorId;
                    var existingTrailerId = driverTripsToday.First().TrailerId;

                    var tractor = tractors.FirstOrDefault(t =>
                        t.TractorId == existingTractorId &&
                        t.MaxLoadWeight >= totalWeight &&
                        t.RegistrationExpirationDate > deliveryDate);

                    var trailer = trailers.FirstOrDefault(t =>
                        t.TrailerId == existingTrailerId &&
                        t.MaxLoadWeight >= totalWeight &&
                        (orderDetail.ContainerSize != 40 || t.ContainerSize == 2) &&
                        t.RegistrationExpirationDate > deliveryDate);

                    if (tractor != null && trailer != null)
                    {
                        return await CreateTrip(orderDetail, driver, tractor, trailer, deliveryDate, completionMinutes, daily, weekly, weekStart, weekEnd);
                    }

                    continue;
                }

                // Nếu chưa có trip hôm đó
                foreach (var tractor in preferredTractors)
                {
                    if (!tractor.MaxLoadWeight.HasValue ||
                        tractor.MaxLoadWeight.Value < totalWeight ||
                        usedTractorIds.Contains(tractor.TractorId) ||
                        tractor.RegistrationExpirationDate <= deliveryDate)
                        continue;

                    foreach (var trailer in trailers.Where(t =>
                        t.MaxLoadWeight >= totalWeight &&
                        (orderDetail.ContainerSize != 40 || t.ContainerSize == 2) &&
                        !usedTrailerIds.Contains(t.TrailerId) &&
                        t.RegistrationExpirationDate > deliveryDate))
                    {
                        usedTractorIds.Add(tractor.TractorId);
                        usedTrailerIds.Add(trailer.TrailerId);

                        return await CreateTrip(orderDetail, driver, tractor, trailer, deliveryDate, completionMinutes, daily, weekly, weekStart, weekEnd);
                    }
                }
            }

            return new BusinessResult(-1, "Không tìm thấy tài xế, đầu kéo hoặc rơ-moóc phù hợp!");
        }



        public async Task<BusinessResult> CreateTrip(MTCS.Data.Models.OrderDetail orderDetail, Driver driver, Tractor tractor, Trailer trailer, DateOnly deliveryDate, int completionMinutes, DriverDailyWorkingTime? daily, DriverWeeklySummary? weekly, DateOnly weekStart, DateOnly weekEnd)
        {
            var trip = new Trip
            {
                TripId = "TRIP" + Guid.NewGuid().ToString("N").Substring(0, 10),
                OrderDetailId = orderDetail.OrderDetailId,
                DriverId = driver.DriverId,
                TractorId = tractor.TractorId,
                TrailerId = trailer.TrailerId,
                Status = "not_started",
                MatchType = 2,
                MatchBy = "System",
                MatchTime = DateTime.Now
            };

            await _unitOfWork.TripRepository.CreateAsync(trip);

            orderDetail.Status = "Scheduled";
            await _unitOfWork.OrderDetailRepository.UpdateAsync(orderDetail);

            if (daily != null)
            {
                daily.TotalTime += completionMinutes;
                daily.ModifiedDate = DateTime.Now;
                await _unitOfWork.DriverDailyWorkingTimeRepository.UpdateAsync(daily);
            }
            else
            {
                await _unitOfWork.DriverDailyWorkingTimeRepository.CreateAsync(new DriverDailyWorkingTime
                {
                    RecordId = Guid.NewGuid().ToString(),
                    DriverId = driver.DriverId,
                    WorkDate = deliveryDate,
                    TotalTime = completionMinutes,
                    CreatedBy = "System",
                    ModifiedDate = DateTime.Now
                });
            }

            if (weekly != null)
            {
                weekly.TotalHours += completionMinutes;
                await _unitOfWork.DriverWeeklySummaryRepository.UpdateAsync(weekly);
            }
            else
            {
                await _unitOfWork.DriverWeeklySummaryRepository.CreateAsync(new DriverWeeklySummary
                {
                    SummaryId = Guid.NewGuid().ToString(),
                    DriverId = driver.DriverId,
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    TotalHours = completionMinutes
                });
            }

            var existingOrder = await _unitOfWork.OrderDetailRepository.GetByIdAsync(trip.OrderDetailId);
            await _notificationService.SendNotificationAsync(trip.DriverId, "Bạn vừa nhận được 1 chuyến hàng mới", $"Chuyến {trip.TripId} bắt đầu từ {existingOrder.PickUpLocation}.", "Hệ thống");

            return new BusinessResult(1, "Tạo trip tự động thành công!", trip);
        }

        #endregion

        #region Get all basic trips
        public async Task<BusinessResult> GetAllTripsAsync()
        {
            try
            {
                var trips = await _unitOfWork.TripRepository.GetAllTripsAsync();

                return new BusinessResult
                {
                    Status = 1,
                    Message = "Success",
                    Data = trips
                };
            }
            catch (Exception ex)
            {
                return new BusinessResult
                {
                    Status = -1,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        #endregion

        public async Task<BusinessResult> GetTripsByGroupAsync(string driverId, string groupType)
        {
            try
            {
                var trips = await _unitOfWork.TripRepository.GetTripsByGroupAsync(driverId, groupType);
                if (trips == null)
                    return new BusinessResult(404, "Trip không tồn tại");
                return new BusinessResult(200, "Success", trips);
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, ex.Message);

            }
        }

        public async Task<ApiResponse<List<TripTimeTable>>> GetTripTimeTable(DateTime startOfWeek, DateTime endOfWeek)
        {
            try
            {
                var timeTable = await _unitOfWork.TripRepository.GetTripTimeTable(startOfWeek, endOfWeek);
                if (timeTable is null || !timeTable.Any())
                {
                    return new ApiResponse<List<TripTimeTable>>(
                        success: true,
                        data: null,
                        message: "Get time table successfully",
                        messageVN: "Không tìm thấy thời gian biểu",
                        errors: null
                    );
                }

                return new ApiResponse<List<TripTimeTable>>(
                    success: true,
                    data: timeTable,
                    message: "Get time table successfully",
                    messageVN: "Lấy thời gian biểu thành công",
                    errors: null
                );
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<TripTimeTable>>(
                    success: false,
                    data: null,
                    message: $"Failed to get time table: {ex.Message}",
                    messageVN: "Không thể lấy thời gian biểu",
                    errors: ex.ToString()
                );
            }
        }
    }
}

