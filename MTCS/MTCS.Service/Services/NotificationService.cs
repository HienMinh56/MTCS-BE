using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using MTCS.Data;
using MTCS.Service.Base;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MTCS.Service.Services
{
    public interface INotificationService
    {
        Task<BusinessResult> SendNotificationAsync(string userId, string title, string body, string userName);
        Task<BusinessResult> SendNotificationWebAsync(string userId, string title, string body, string userName);
    }

    public class NotificationService : INotificationService
    {
        private readonly IFCMService _fcmService;
        private readonly ILogger<NotificationService> _logger;
        private readonly FirestoreDb _firestoreDb;
        private readonly UnitOfWork _unitOfWork;


        public NotificationService(IFCMService fcmService, ILogger<NotificationService> logger, FirestoreDb firestoreDb, UnitOfWork unitOfWork)
        {
            _fcmService = fcmService;
            _logger = logger;
            _firestoreDb = firestoreDb;
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessResult> SendNotificationAsync(string userId, string title, string body, string userName)
        {

            var userDoc = await _firestoreDb.Collection("users").Document(userId).GetSnapshotAsync();
            if (!userDoc.Exists || !userDoc.ContainsField("fcmToken"))
            {
                return await SendNotificationWebAsync(userId, title, body, userName);
            }

            var fcmToken = userDoc.GetValue<string>("fcmToken");

            var message = new Message
            {
                Token = fcmToken,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>
        {
            { "click_action", "FLUTTER_NOTIFICATION_CLICK" },
            { "userId", userId }
        }
            };

            try
            {
                var messaging = FirebaseMessaging.DefaultInstance;
                await messaging.SendAsync(message);
            }
            catch (FirebaseMessagingException ex)
            {
                if (ex.ErrorCode == ErrorCode.NotFound || ex.ErrorCode == ErrorCode.InvalidArgument)
                {
                    await _firestoreDb.Collection("users").Document(userId).UpdateAsync(new Dictionary<string, object>
            {
                { "fcmToken", FieldValue.Delete }
            });
                }
            }

            var notificationRef = _firestoreDb.Collection("Notifications").Document();
            await notificationRef.SetAsync(new
            {
                UserId = userId,
                Title = title,
                Body = body,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                Sender = userName,
                isRead = false
            });

            return new BusinessResult(200, "Notification processed (saved to Firestore)");
        }




        public async Task<BusinessResult> SendNotificationWebAsync(string userId, string title, string body, string userName)
        {

            // Save notification in Firestore
            try
            {
                var notificationRef = _firestoreDb.Collection("Notifications").Document();
                await notificationRef.SetAsync(new
                {
                    UserId = userId,
                    Title = title,
                    Body = body,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    Sender = userName,
                    isRead = false
                });

                return new BusinessResult(200, "Notification saved to Firestore successfully");
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, $"Error saving notification: {ex.Message}");
            }
        }


    }
}
