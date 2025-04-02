using System.Security.Claims;
using MTCS.Data.DTOs;
using MTCS.Data.Response;
using MTCS.Service.Base;

namespace MTCS.Service.Interfaces
{
    public interface ITripService
    {
        Task<BusinessResult> GetTripsByFilterAsync(string? tripId, string? driverId, string? status, string? tractorId, string? trailerId, string? orderId);
        Task<BusinessResult> UpdateStatusTrip(string tripId, string newStatusId, string userId);
    }
}
