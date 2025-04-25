using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.SignalR;
using MTCS.Service.Services;

namespace MTCS.Service.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Khi driver gửi tin nhắn cho staff
        public async Task SendMessageToStaff(string driverId, string staffId, string message)
        {
            var senderId = driverId; // Hoặc lấy từ Context.User.Identity nếu đã gắn Claims
            var messageId =  _chatService.SaveMessageAsync(driverId, staffId, senderId, message);

            // Gửi đến staff thông qua SignalR group
            await Clients.User(staffId).SendAsync("ReceiveMessage", driverId, message, messageId, "sent");
        }

        // Khi staff gửi tin nhắn cho driver
        public async Task ReplyToDriver(string staffId, string driverId, string message)
        {
            var senderId = staffId;
            var messageId =  _chatService.SaveMessageAsync(driverId, staffId, senderId, message);

            await Clients.User(driverId).SendAsync("ReceiveMessage", staffId, message, messageId, "sent");
        }

        // Khi user (driver hoặc staff) đã đọc tin nhắn
        public async Task MarkMessageAsRead(string driverId, string staffId, string messageId)
        {
            await _chatService.MarkMessageAsReadAsync(driverId, staffId, messageId);

            // Gửi thông báo đến người gửi về việc tin nhắn đã được đọc
            await Clients.User(driverId).SendAsync("MessageRead", messageId, Timestamp.GetCurrentTimestamp());
            await Clients.User(staffId).SendAsync("MessageRead", messageId, Timestamp.GetCurrentTimestamp());
        }
    }

}
