using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> RegisterStaff(RegisterUserDTO userDto);
        Task<ApiResponse<string>> RegisterAdmin(RegisterUserDTO userDto);
        Task<ApiResponse<TokenDTO>> LoginInternalUser(LoginRequestDTO loginDto);
        Task<ApiResponse<TokenDTO>> LoginDriver(LoginRequestDTO loginDto);
        Task<ApiResponse<ProfileResponseDTO>> GetUserProfile(string userId);
        Task<ApiResponse<ProfileResponseDTO>> UpdateInternalUserProfile(string userId, ProfileDTO profileDto);
        Task<ApiResponse<string>> ChangeUserActivationStatus(string userId, int newStatus, string modifierId);
        Task<ApiResponse<PagedList<InternalUser>>> GetInternalUserWithFilter(PaginationParams paginationParams, string? keyword = null, int? role = null);
        Task<ApiResponse<ProfileResponseDTO>> UpdateUserInformation(string userId, AdminUpdateUserDTO updateDto, string modifierId);
    }
}
