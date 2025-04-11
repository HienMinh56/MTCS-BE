using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        // Lưu kết nối của app mobile (driver)
        private static readonly Dictionary<string, WebSocket> _userSockets = new();

        // Lưu vị trí hiện tại của mỗi userId
        private static readonly Dictionary<string, LocationData> _userLocations = new();

        // Lưu danh sách các client subscribe theo userId (web client)
        private static readonly Dictionary<string, List<WebSocket>> _subscribers = new();

        public static async Task Handle(HttpContext context, WebSocket socket)
        {
            var query = context.Request.Query;
            var userId = query["userId"].ToString();
            var token = query["token"].ToString();
            var action = query["action"].ToString(); // "send" | "subscribe"

            var handler = new WebSocketHandler(context.RequestServices.GetService<IConfiguration>());

            // Nếu là gửi vị trí từ mobile (phải xác thực)
            if (action == "send")
            {
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

                            // Gửi vị trí đến tất cả subscriber đang theo dõi userId này
                            if (_subscribers.TryGetValue(userId, out var subs))
                            {
                                var message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(location));
                                foreach (var subSocket in subs.ToList())
                                {
                                    if (subSocket.State == WebSocketState.Open)
                                    {
                                        await subSocket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
                                    }
                                    else
                                    {
                                        subs.Remove(subSocket); // loại bỏ nếu đã đóng
                                    }
                                }
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _userSockets.Remove(userId);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                    }
                }

                return;
            }

            // Nếu là client chỉ muốn subscribe (web client) – không cần token
            if (action == "subscribe")
            {
                if (!_subscribers.ContainsKey(userId))
                    _subscribers[userId] = new List<WebSocket>();
                _subscribers[userId].Add(socket);

                var buffer = new byte[1024 * 4];
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _subscribers[userId].Remove(socket);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                    }
                }

                return;
            }

            // Nếu action không hợp lệ
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid action", CancellationToken.None);
        }

        public static LocationData? GetLocationForUser(string userId)
        {
            _userLocations.TryGetValue(userId, out var location);
            return location;
        }

        private bool ValidateToken(string userId, string token)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]);
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
