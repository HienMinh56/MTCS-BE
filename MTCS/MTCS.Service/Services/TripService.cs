using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public TripService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }

        #region GetTripsByFilter
        public async Task<BusinessResult> GetTripsByFilterAsync(string? tripId, string? driverId, string? status, string? tractorId, string? trailerId, string? orderId)
        {
            try
            {
                var trips = await _unitOfWork.TripRepository.GetTripsByFilterAsync(tripId,driverId, status, tractorId, trailerId, orderId);

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
                    await UpdateOrderAndVehiclesAsync(trip, "Delivering", VehicleStatus.OnDuty, DriverStatus.OnDuty);
                }

                trip.Status = newStatusId;

                if (newStatus.StatusIndex == higherSecondStatus.StatusIndex + 1)
                {
                    trip.EndTime = DateTime.Now;
                    await UpdateOrderAndVehiclesAsync(trip, "Completed", VehicleStatus.Active, DriverStatus.Active);
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

        public async Task<BusinessResult> UpdateTripAsync(string tripId, UpdateTripRequest model, ClaimsPrincipal claims)
        {
            try
            {
                var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Staff";

                await _unitOfWork.BeginTransactionAsync();
                var oldTrip = await _unitOfWork.TripRepository.GetByIdAsync(tripId);
                if (oldTrip == null)
                    return new BusinessResult(Const.FAIL_READ_CODE, "Trip không tồn tại");

                // check MaxloadWeight xe moi phai >= xe cu
                if (!string.IsNullOrEmpty(model.TrailerId) && model.TrailerId != oldTrip.TrailerId)
                {
                    var oldTrailer = await _unitOfWork.TrailerRepository.GetByIdAsync(oldTrip.TrailerId);
                    var newTrailer = await _unitOfWork.TrailerRepository.GetByIdAsync(model.TrailerId);
                    if (newTrailer == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Trailer mới không tồn tại");
                    if (newTrailer.MaxLoadWeight < oldTrailer.MaxLoadWeight)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tải trọng của Trailer mới không phù hợp");
                }

                // check MaxloadWeight xe moi phai >= xe cu
                if (!string.IsNullOrEmpty(model.TractorId) && model.TractorId != oldTrip.TractorId)
                {
                    var oldTractor = await _unitOfWork.TractorRepository.GetByIdAsync(oldTrip.TractorId);
                    var newTractor = await _unitOfWork.TractorRepository.GetByIdAsync(model.TractorId);
                    if (newTractor == null)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tractor mới không tồn tại");
                    if (newTractor.MaxLoadWeight < oldTractor.MaxLoadWeight)
                        return new BusinessResult(Const.FAIL_UPDATE_CODE, "Tải trọng của Tractor mới không phù hợp");
                }

                var newTrip = new Trip
                {
                    TripId = Guid.NewGuid().ToString(), 
                    OrderId = oldTrip.OrderId,
                    DriverId = model.DriverId ?? oldTrip.DriverId, 
                    TractorId = model.TractorId ?? oldTrip.TractorId,  
                    TrailerId = model.TrailerId ?? oldTrip.TrailerId, 
                    Status = model.Status ?? oldTrip.Status,
                    StartTime = DateTime.Now,
                    MatchType = 2,
                    MatchBy = userName,
                    MatchTime = DateTime.Now,                                 
                };

                await _unitOfWork.TripRepository.CreateAsync(newTrip);

                oldTrip.Status = "cancel";
                await _unitOfWork.TripRepository.UpdateAsync(oldTrip);

                await _unitOfWork.CommitTransactionAsync();

                return new BusinessResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật thành công", newTrip);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(Const.FAIL_UPDATE_CODE, ex.Message);
            }
        }
    }
}
