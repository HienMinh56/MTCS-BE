using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Api;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MTCS.Service.Hubs
{
    public class LocationHub : Hub
    {
        private static readonly Dictionary<string, LocationData> _userLocations = new();
        private readonly IConfiguration _configuration;

        public LocationHub(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Driver gửi vị trí
        /// </summary>
        public async Task SendLocation(string token, double latitude, double longitude)
        {
            var userId = ValidateTokenAndGetUserId(token);
            if (string.IsNullOrEmpty(userId))
            {
                // Đóng kết nối nếu không hợp lệ
                Context.Abort();
                return;
            }

            var location = new LocationData { Latitude = latitude, Longitude = longitude };
            _userLocations[userId] = location;

            // Gửi vị trí đến các client đang theo dõi userId này
            await Clients.Group(userId).SendAsync("ReceiveLocation", userId, latitude, longitude);
        }

        /// <summary>
        /// Web client join group để lắng nghe vị trí userId
        /// </summary>
        public async Task Subscribe(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        /// <summary>
        /// Web client bỏ theo dõi
        /// </summary>
        public async Task Unsubscribe(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        private string? ValidateTokenAndGetUserId(string token)
        {
            try
            {
                var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]);
                var tokenHandler = new JwtSecurityTokenHandler();
                var issuer = _configuration["JwtSettings:Issuer"];
                var audience = _configuration["JwtSettings:Audience"];

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, parameters, out var validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;

                return jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
            }
            catch
            {
                return null;
            }
        }

        public class LocationData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
