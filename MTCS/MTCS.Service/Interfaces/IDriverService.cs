using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IDriverService
    {
        Task<ApiResponse<string>> CreateDriver(CreateDriverDTO driverDto);
        Task<ApiResponse<DriverProfileDetailsDTO>> GetDriverProfile(string driverId);
        Task<ApiResponse<PagedList<ViewDriverDTO>>> ViewDrivers(PaginationParams paginationParams, int? status = null, string? keyword = null);
    }
}
