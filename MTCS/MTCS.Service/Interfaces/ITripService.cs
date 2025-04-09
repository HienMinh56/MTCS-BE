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
        Task<BusinessResult> UpdateStatusTrip(string tripId, string newStatusId, string userId);
        Task<BusinessResult> UpdateTripAsync(string tripId, UpdateTripRequest model, ClaimsPrincipal claims);
        Task<BusinessResult> CreateTripAsync(CreateTripRequestModel tripRequestModel, ClaimsPrincipal claims);
    }
}
