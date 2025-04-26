using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using MTCS.Data;
using Org.BouncyCastle.Tls;

namespace MTCS.Service.Services
{
    public interface IChatService
    {
        Task SendMessageAsync(string senderId, string receiverId, string message);
    }
    public class ChatService : IChatService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFCMService _fcmService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IFCMService fcmService, ILogger<ChatService> logger, UnitOfWork unitOfWork)
        {
            _fcmService = fcmService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task SendMessageAsync(string senderId, string receiverId, string message)
        {
            // Lấy thông tin người gửi (có thể là staff hoặc driver)
            var senderInternal = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(senderId);
            var senderDriver = senderInternal == null
                ? await _unitOfWork.DriverRepository.GetDriverByIdAsync(senderId)
                : null;

            if (senderInternal == null && senderDriver == null)
            {
                throw new Exception("Sender not found.");
            }

            var senderName = senderInternal?.FullName ?? senderDriver?.FullName;
            if (string.IsNullOrEmpty(senderName))
            {
                throw new Exception("Sender name is invalid.");
            }

            // Lấy thông tin người nhận (có thể là staff hoặc driver)
            var receiverInternal = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(receiverId);
            var receiverDriver = receiverInternal == null
                ? await _unitOfWork.DriverRepository.GetDriverByIdAsync(receiverId)
                : null;

            if (receiverInternal == null && receiverDriver == null)
            {
                throw new Exception("Receiver not found.");
            }

            var receiverName = receiverInternal?.FullName ?? receiverDriver?.FullName;
            if (string.IsNullOrEmpty(receiverName))
            {
                throw new Exception("Receiver name is invalid.");
            }

            var db = _fcmService.GetFirestoreDb();


            var chatId = string.Compare(senderId, receiverId) < 0
                ? $"{senderId}_{receiverId}"
                : $"{receiverId}_{senderId}";

            var chatDoc = db.Collection("chats").Document(chatId);

            var participants = new[]
            {
                 new { id = senderId, name = senderName },
                new { id = receiverId, name = receiverName }
            };

            await chatDoc.SetAsync(new { participants }, SetOptions.MergeAll);


            var messageDoc = chatDoc.Collection("messages").Document();
            await messageDoc.SetAsync(new
            {
                senderId,
                senderName,
                receiverId,
                receiverName,
                text = message,
                timestamp = Timestamp.GetCurrentTimestamp(),
                read = false
            });


            await SendFCMNotificationAsync(receiverId, senderId, senderName, message);
        }

        private async Task SendFCMNotificationAsync(string receiverId, string senderId, string senderName, string message)
        {
            var db = _fcmService.GetFirestoreDb();
            var userDoc = await db.Collection("users").Document(receiverId).GetSnapshotAsync();


            if (!userDoc.Exists || !userDoc.ContainsField("fcmToken"))
            {
                return;
            }

            var fcmToken = userDoc.GetValue<string>("fcmToken");

            var fcm = _fcmService.GetMessagingClient();

            var notification = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = senderName,
                    Body = message
                },
                Data = new Dictionary<string, string>
        {
            { "senderId", senderId }
        }
            };

            await fcm.SendAsync(notification);
        }

    }
}
