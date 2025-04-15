using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IDriverService
    {
        Task<ApiResponse<bool>> ActivateDriver(string driverId, string userName);
        Task<ApiResponse<DriverResponseDTO>> CreateDriverWithFiles(CreateDriverDTO driverDto, List<FileUploadDTO> fileUploads, string userId);
        Task<ApiResponse<bool>> DeactivateDriver(string driverId, string userName);
        Task<ApiResponse<bool>> DeleteDriver(string driverId, string userName);
        Task<ApiResponse<DriverProfileDetailsDTO>> GetDriverProfile(string driverId);
        Task<ApiResponse<bool>> UpdateDriverFileDetails(string fileId, FileDetailsDTO updateDto, string userId);
        Task<ApiResponse<DriverResponseDTO>> UpdateDriverWithFiles(string driverId, UpdateDriverDTO updateDto, List<FileUploadDTO> newFiles, List<string> fileIdsToRemove, string userId);
        Task<ApiResponse<PagedList<ViewDriverDTO>>> ViewDrivers(PaginationParams paginationParams, int? status = null, string? keyword = null);
    }
}
