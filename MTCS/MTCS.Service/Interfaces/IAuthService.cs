using MTCS.Data.DTOs;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<string>> RegisterCustomerAsync(RegisterUserDTO userDto);
        Task<ApiResponse<TokenDTO>> LoginUserAsync(LoginRequestDTO loginDto);
        Task<ApiResponse<TokenDTO>> LoginDriverAsync(LoginRequestDTO loginDto);
        Task<ApiResponse<ProfileResponseDTO>> UpdateUserProfile(string userId, ProfileDTO profileDto);
    }
}
