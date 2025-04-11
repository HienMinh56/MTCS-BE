using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MTCS.Service.Handler
{
    public class WebSocketHandler
    {
        private readonly IConfiguration _configuration;

        public WebSocketHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // Lưu kết nối theo userId
        private static readonly Dictionary<string, WebSocket> _userSockets = new();

        // Lưu vị trí hiện tại của từng userId
        private static readonly Dictionary<string, LocationData> _userLocations = new();

        public static async Task Handle(HttpContext context, WebSocket socket)
        {
            var query = context.Request.Query;
            var userId = query["userId"].ToString();
            var token = query["token"].ToString();

            var handler = new WebSocketHandler(context.RequestServices.GetService<IConfiguration>());

            if (string.IsNullOrEmpty(userId) || !handler.ValidateToken(userId, token))
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
                return;
            }

            _userSockets[userId] = socket;

            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var location = JsonSerializer.Deserialize<LocationData>(json);

                    if (location != null)
                    {
                        _userLocations[userId] = location;
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _userSockets.Remove(userId);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
        }

        // Trả về vị trí người dùng theo userId (dùng từ controller hoặc web client)
        public static LocationData? GetLocationForUser(string userId)
        {
            _userLocations.TryGetValue(userId, out var location);
            return location;
        }

        // Đơn giản hóa xác thực token (nên thay bằng logic thực tế hoặc JWT check)
        private bool ValidateToken(string userId, string token)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]); // lấy từ IConfiguration
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var tokenUserId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                return tokenUserId == userId;
            }
            catch
            {
                return false;
            }
        }


        public class LocationData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
