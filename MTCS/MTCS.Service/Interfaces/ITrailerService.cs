using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITrailerService
    {
        Task<ApiResponse<TrailerResponseDTO>> CreateTrailer(CreateTrailerDTO trailerDto, string userId);
        Task<ApiResponse<TrailerBasicInfoResultDTO>> GetTrailersBasicInfo(
           PaginationParams paginationParams,
           string? searchKeyword = null,
           TrailerStatus? status = null,
           bool? maintenanceDueSoon = null,
           bool? registrationExpiringSoon = null,
           int? maintenanceDueDays = null,
           int? registrationExpiringDays = null);
        Task<ApiResponse<TrailerDetailsDTO>> GetTrailerDetail(string trailerId);
        Task<ApiResponse<bool>> DeleteTrailer(string trailerId, string userId);
        Task<ApiResponse<bool>> ActivateTrailer(string trailerId, string userId);
    }
}
