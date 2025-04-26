using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace MTCS.Service.Services
{
    public interface IChatService
    {
        Task SendMessageAsync(string senderId, string receiverId, string message);
    }
    public class ChatService : IChatService
    {
        private readonly IFCMService _fcmService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IFCMService fcmService, ILogger<ChatService> logger)
        {
            _fcmService = fcmService;
            _logger = logger;
        }

        public async Task SendMessageAsync(string senderId, string receiverId, string message)
        {
            var db = _fcmService.GetFirestoreDb();

            // Chat ID được tạo dựa trên 2 userId theo thứ tự chữ cái
            var chatId = string.Compare(senderId, receiverId) < 0
                ? $"{senderId}_{receiverId}"
                : $"{receiverId}_{senderId}";

            var chatDoc = db.Collection("chats").Document(chatId);

            // Đảm bảo participants có tồn tại (nếu là lần đầu chat)
            await chatDoc.SetAsync(new { participants = new[] { senderId, receiverId } }, SetOptions.MergeAll);

            // Ghi message vào Firestore
            var messageDoc = chatDoc.Collection("messages").Document();
            await messageDoc.SetAsync(new
            {
                senderId,
                receiverId,
                text = message,
                timestamp = Timestamp.GetCurrentTimestamp(),
                read = false
            });

            _logger.LogInformation($"💬 Message sent from {senderId} to {receiverId}");

            // 🔔 Gửi FCM notification
            await SendFCMNotificationAsync(receiverId, senderId, message);
        }

        private async Task SendFCMNotificationAsync(string receiverId, string senderId, string message)
        {
            var db = _fcmService.GetFirestoreDb();
            var userDoc = await db.Collection("users").Document(receiverId).GetSnapshotAsync();

            if (!userDoc.Exists || !userDoc.ContainsField("fcmToken"))
            {
                _logger.LogWarning($"⚠️ No FCM token for {receiverId}, message will be delivered silently.");
                return; // Không gửi FCM, nhưng tin nhắn vẫn được lưu Firestore
            }

            var fcmToken = userDoc.GetValue<string>("fcmToken");

            var fcm = _fcmService.GetMessagingClient();

            var notification = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = "Tin nhắn mới",
                    Body = message
                },
                Data = new Dictionary<string, string>
        {
            { "senderId", senderId }
        }
            };

            await fcm.SendAsync(notification);
            _logger.LogInformation($"🔔 Sent message notification to {receiverId}");
        }

    }
}
