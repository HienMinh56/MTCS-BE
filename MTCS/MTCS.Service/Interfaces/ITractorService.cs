using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITractorService
    {
        Task<ApiResponse<TractorResponseDTO>> CreateTractor(CreateTractorDTO tractorDto, string userId);
        Task<ApiResponse<TractorBasicInfoResultDTO>> GetTractorsBasicInfo(
           PaginationParams paginationParams,
           string? searchKeyword = null,
           TractorStatus? status = null,
           bool? maintenanceDueSoon = null,
           bool? registrationExpiringSoon = null,
           int? maintenanceDueDays = null,
           int? registrationExpiringDays = null);
        Task<ApiResponse<TractorDetailsDTO>> GetTractorDetail(string tractorId);
        Task<ApiResponse<bool>> DeleteTractor(string tractorId, string userId);
        Task<ApiResponse<bool>> ActivateTractor(string tractorId, string userId);
    }
}
