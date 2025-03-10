using MTCS.Data.DTOs;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITripService
    {
        Task<ApiResponse<List<TripDTO>>> GetDriverAssignedTrips(string driverId);
        Task<ApiResponse<DetailedTripDTO>> GetTripDetails(string tripId);
    }
}
