using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MTCS.Service.Services
{
    public class TokenService : ITokenService
    {
        private readonly JWTSettings _jwtSettings;
        private readonly IConfiguration _configuration;
        private readonly UnitOfWork _unitOfWork;
        private readonly int _accessExpiryMinutes;
        private readonly int _refreshExpiryDays;

        public TokenService(IOptions<JWTSettings> jwtSettings, IConfiguration configuration, UnitOfWork unitOfWork)
        {
            _jwtSettings = jwtSettings.Value;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _accessExpiryMinutes = _configuration.GetValue<int>("JwtSettings:AccessExpiryMinutes");
            _refreshExpiryDays = _configuration.GetSection("JwtSettings").GetValue<int>("RefreshExpiryDays");
        }

        public Task<TokenDTO> GenerateTokensForUserAsync(User user)
        {
            var accessToken = GenerateAccessTokenForUserAsync(user);
            var refreshToken = GenerateRefreshTokenForUserAsync(user);

            return Task.FromResult(new TokenDTO
            {
                Token = accessToken.Result.Token,
                RefreshToken = refreshToken.Result.Token,
            });
        }

        public Task<TokenDTO> GenerateAccessTokenForUserAsync(User user)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Add role claim if available
            if (!string.IsNullOrEmpty(user.Role))
            {
                authClaims.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            var authSigningKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_accessExpiryMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        public Task<TokenDTO> GenerateRefreshTokenForUserAsync(User user)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var authSigningKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddDays(_refreshExpiryDays),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }


        #region Driver Token Methods

        public Task<TokenDTO> GenerateTokensForDriverAsync(Driver driver)
        {
            var accessToken = GenerateAccessTokenForDriverAsync(driver);
            var refreshToken = GenerateRefreshTokenForDriverAsync(driver);

            return Task.FromResult(new TokenDTO
            {
                Token = accessToken.Result.Token,
                RefreshToken = refreshToken.Result.Token,
            });
        }

        public Task<TokenDTO> GenerateAccessTokenForDriverAsync(Driver driver)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, driver.DriverId),
                new Claim(ClaimTypes.Name, driver.FullName ?? ""),
                new Claim(ClaimTypes.Email, driver.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                //add a default "Driver" role claim for authorization
                new Claim(ClaimTypes.Role, "Driver")
            };

            var authSigningKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_accessExpiryMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        public Task<TokenDTO> GenerateRefreshTokenForDriverAsync(Driver driver)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, driver.DriverId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var authSigningKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddDays(_refreshExpiryDays),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        #endregion

        public async Task<ApiResponse<TokenDTO>> RefreshTokenAsync(string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            try
            {
                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new ApiResponse<TokenDTO>(false, null, "Invalid token", null);
                }

                var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var role = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                TokenDTO newTokens;

                if (role == Role.Driver.Name)
                {
                    var driver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(userId);
                    if (driver == null)
                    {
                        return new ApiResponse<TokenDTO>(false, null, "Driver not found", null);
                    }

                    newTokens = await GenerateTokensForDriverAsync(driver);
                }
                else
                {
                    var user = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
                    if (user == null)
                    {
                        return new ApiResponse<TokenDTO>(false, null, "User not found", null);
                    }

                    newTokens = await GenerateTokensForUserAsync(user);
                }
                return new ApiResponse<TokenDTO>(true, newTokens, "Token refreshed successfully", null);
            }
            catch (SecurityTokenExpiredException)
            {
                return new ApiResponse<TokenDTO>(false, null, "Token expired", "Refresh token has expired");
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenDTO>(false, null, "Token validation failed", ex.Message);
            }
        }
    }
}

