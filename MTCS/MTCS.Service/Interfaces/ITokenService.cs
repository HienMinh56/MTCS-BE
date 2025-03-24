using MTCS.Data.DTOs;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITokenService
    {
        Task<TokenDTO> GenerateTokensForInternalUser(InternalUser user);
        Task<TokenDTO> GenerateAccessTokenForInternalUser(InternalUser user);
        Task<TokenDTO> GenerateRefreshTokenForInternalUser(InternalUser user);

        Task<TokenDTO> GenerateTokensForDriver(Driver driver);
        Task<TokenDTO> GenerateAccessTokenForDriver(Driver driver);
        Task<TokenDTO> GenerateRefreshTokenForDriver(Driver driver);

        Task<ApiResponse<TokenDTO>> RefreshToken(string refreshToken);
    }
}
