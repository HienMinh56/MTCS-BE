using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IDriverService
    {
        Task<ApiResponse<DriverResponseDTO>> CreateDriverWithFiles(CreateDriverDTO driverDto, List<FileUploadDTO> fileUploads, string userId);
        Task<ApiResponse<DriverProfileDetailsDTO>> GetDriverProfile(string driverId);
        Task<ApiResponse<PagedList<ViewDriverDTO>>> ViewDrivers(PaginationParams paginationParams, int? status = null, string? keyword = null);
    }
}
