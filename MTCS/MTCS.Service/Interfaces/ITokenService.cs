using MTCS.Data.DTOs;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITokenService
    {
        //Task<TokenDTO> GenerateTokensForUserAsync(User user);
        //Task<TokenDTO> GenerateAccessTokenForUserAsync(User user);
        //Task<TokenDTO> GenerateRefreshTokenForUserAsync(User user);

        Task<TokenDTO> GenerateTokensForDriverAsync(Driver driver);
        Task<TokenDTO> GenerateAccessTokenForDriverAsync(Driver driver);
        Task<TokenDTO> GenerateRefreshTokenForDriverAsync(Driver driver);

        //Task<ApiResponse<TokenDTO>> RefreshTokenAsync(string refreshToken);
    }
}
