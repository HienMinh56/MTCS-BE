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

        public Task<TokenDTO> GenerateTokensForInternalUser(InternalUser user)
        {
            var accessToken = GenerateAccessTokenForInternalUser(user);
            var refreshToken = GenerateRefreshTokenForInternalUser(user);

            return Task.FromResult(new TokenDTO
            {
                Token = accessToken.Result.Token,
                RefreshToken = refreshToken.Result.Token,
            });
        }

        public Task<TokenDTO> GenerateAccessTokenForInternalUser(InternalUser user)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
                new Claim(ClaimTypes.Name, user.FullName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (user.Role.HasValue)
            {
                // Convert int to enum and get role name as string
                if (Enum.IsDefined(typeof(InternalUserRole), user.Role.Value))
                {
                    var roleName = ((InternalUserRole)user.Role.Value).ToString();
                    authClaims.Add(new Claim(ClaimTypes.Role, roleName));
                }
                else
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, "Unknown"));
                }
            }

            var authSigningKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.Now.AddMinutes(_accessExpiryMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        public Task<TokenDTO> GenerateRefreshTokenForInternalUser(InternalUser user)
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
                expires: DateTime.Now.AddDays(_refreshExpiryDays),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        #region Driver Token Methods

        public Task<TokenDTO> GenerateTokensForDriver(Driver driver)
        {
            var accessToken = GenerateAccessTokenForDriver(driver);
            var refreshToken = GenerateRefreshTokenForDriver(driver);

            return Task.FromResult(new TokenDTO
            {
                Token = accessToken.Result.Token,
                RefreshToken = refreshToken.Result.Token,
            });
        }

        public Task<TokenDTO> GenerateAccessTokenForDriver(Driver driver)
        {
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, driver.DriverId),
                new Claim(ClaimTypes.Name, driver.FullName ?? ""),
                new Claim(ClaimTypes.Email, driver.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                //add a default "Driver" role claim
                new Claim(ClaimTypes.Role, "Driver")
            };

            var authSigningKey = new SymmetricSecurityKey(key);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.Now.AddMonths(_accessExpiryMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        public Task<TokenDTO> GenerateRefreshTokenForDriver(Driver driver)
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
                expires: DateTime.Now.AddYears(_refreshExpiryDays),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Task.FromResult(new TokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
            });
        }

        #endregion

        public async Task<ApiResponse<TokenDTO>> RefreshToken(string refreshToken)
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
                    return new ApiResponse<TokenDTO>(false, null, "Invalid token", null, null);
                }

                var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var role = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                TokenDTO newTokens;

                if (role == "Driver")
                {
                    var driver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(userId);
                    if (driver == null)
                    {
                        return new ApiResponse<TokenDTO>(false, null, "Driver not found", "Không tìm thấy tài xế", null);
                    }

                    newTokens = await GenerateTokensForDriver(driver);
                }
                else
                {
                    // For Staff and Admin roles (InternalUser)
                    var internalUser = await _unitOfWork.InternalUserRepository.GetByIdAsync(userId);
                    if (internalUser == null)
                    {
                        return new ApiResponse<TokenDTO>(false, null, "User not found", "không tìm thấy người dùng", null);
                    }

                    newTokens = await GenerateTokensForInternalUser(internalUser);
                }
                return new ApiResponse<TokenDTO>(true, newTokens, "Token refreshed successfully", null, null);
            }
            catch (SecurityTokenExpiredException)
            {
                return new ApiResponse<TokenDTO>(false, null, "Token expired", null, "Refresh token has expired");
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenDTO>(false, null, "Token validation failed", null, ex.Message);
            }
        }
    }
}

