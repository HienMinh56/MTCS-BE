using System.Security.Claims;
using MTCS.Data.DTOs;
using MTCS.Data.Models;
using MTCS.Data.Request;
using MTCS.Data.Response;
using MTCS.Service.Base;

namespace MTCS.Service.Interfaces
{
    public interface ITripService
    {
        Task<BusinessResult> GetTripsByFilterAsync(string? tripId, string? driverId, string? status, string? tractorId, string? trailerId, string? orderId, string? trackingCode, string? tractorlicensePlate, string? trailerlicensePlate);
        Task<BusinessResult> GetTripsByGroupAsync(string driverId, string groupType);
        Task<BusinessResult> UpdateStatusTrip(string tripId, string newStatusId, string userId);
        Task<BusinessResult> UpdateTripAsync(string tripId, UpdateTripRequest model, ClaimsPrincipal claims);
        Task<BusinessResult> CreateTripAsync(CreateTripRequestModel tripRequestModel, ClaimsPrincipal claims);
        Task<BusinessResult> AutoScheduleTripsForOrderAsync(string orderId);
        Task<IBusinessResult> CancelTrip(CancelTripRequest request ,ClaimsPrincipal claims);
        Task<BusinessResult> CreateTrip(MTCS.Data.Models.Order order, Driver driver, Tractor tractor, Trailer trailer, DateOnly deliveryDate, int completionMinutes, DriverDailyWorkingTime? daily, DriverWeeklySummary? weekly, DateOnly weekStart, DateOnly weekEnd);
        Task<BusinessResult> GetAllTripsAsync();

    }
}
