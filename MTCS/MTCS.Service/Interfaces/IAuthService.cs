using MTCS.Data.DTOs;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> RegisterStaff(RegisterUserDTO userDto);
        Task<ApiResponse<string>> RegisterAdmin(RegisterUserDTO userDto);
        Task<ApiResponse<TokenDTO>> LoginInternalUser(LoginRequestDTO loginDto);
        Task<ApiResponse<TokenDTO>> LoginDriver(LoginRequestDTO loginDto);
        Task<ApiResponse<ProfileResponseDTO>> UpdateInternalUserProfile(string userId, ProfileDTO profileDto);
    }
}
