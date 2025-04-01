using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Models;
using MTCS.Data.Repository;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class TripService : ITripService
    {
        private readonly UnitOfWork _unitOfWork;

        public TripService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }

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

        public async Task<BusinessResult> UpdateStatusTrip(string tripId, string newStatusId, string userId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var trip =  _unitOfWork.TripRepository.Get(t => t.TripId == tripId);
                var higherSecondStatus = await _unitOfWork.DeliveryStatusRepository.GetSecondHighestStatusIndexAsync();
                var beingReport = _unitOfWork.IncidentReportsRepository.Get(i => i.TripId == tripId && i.Status == "Handling");
                if (beingReport != null)
                {
                    return new BusinessResult(400, "Cannot update status as there is an incident report being handled");
                }

                if (trip == null)
                {
                    return new BusinessResult(404, "Trip Not Found");
                }

                var currentStatus = await _unitOfWork.DeliveryStatusRepository.GetByIdAsync(trip.Status);
                var newStatus = await _unitOfWork.DeliveryStatusRepository.GetByIdAsync(newStatusId);

                if (newStatus == null)
                {
                    return new BusinessResult(404, "Status not existed"); 
                }

                if (currentStatus != null)
                {
                    if (newStatus.StatusIndex != currentStatus.StatusIndex + 1)
                    {
                        return new BusinessResult(400, "Cannot update as over step status"); 
                    }
                }


                trip.Status = newStatusId;
                if(newStatus.StatusIndex == higherSecondStatus.StatusIndex + 1)
                {
                    trip.EndTime = DateTime.UtcNow;
                }
                await _unitOfWork.TripRepository.UpdateAsync(trip);

                var tripStatusHistory = new TripStatusHistory
                {
                    HistoryId = Guid.NewGuid().ToString(),
                    TripId = tripId,
                    StatusId = newStatusId,
                    StartTime = DateTime.UtcNow
                };

                await _unitOfWork.TripStatusHistoryRepository.CreateAsync(tripStatusHistory);
                return new BusinessResult(200, "Update Trip Success");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new BusinessResult(500, "Internal Server Error");
            }
        }
    }
}
