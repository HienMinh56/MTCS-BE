using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using MTCS.Service.Base;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MTCS.Service
{
    public interface INotificationService
    {
        Task<BusinessResult> SendNotificationAsync(string userId, string title, string body, ClaimsPrincipal claims);
    }

    public class NotificationService : INotificationService
    {
        private readonly IFCMService _fcmService;
        private readonly ILogger<NotificationService> _logger;
        private readonly FirestoreDb _firestoreDb;


        public NotificationService(IFCMService fcmService, ILogger<NotificationService> logger, FirestoreDb firestoreDb)
        {
            _fcmService = fcmService;
            _logger = logger;
            _firestoreDb = firestoreDb;
        }

        public async Task<BusinessResult> SendNotificationAsync(string userId, string title, string body, ClaimsPrincipal claims)
        {

            var userName = claims.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            var userDoc = await _firestoreDb.Collection("users").Document(userId).GetSnapshotAsync();
            if (!userDoc.Exists || !userDoc.ContainsField("fcmToken"))
            {
                return new BusinessResult(404, $"{userId} does not have FCM Token!!!");
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
                var response = await messaging.SendAsync(message);

                var notificationRef = _firestoreDb.Collection("Notifications").Document();
                await notificationRef.SetAsync(new
                {
                    UserId = userId,
                    Title = title,
                    Body = body,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    Sender = userName
                });

                Console.WriteLine("✅ Notification saved to Firestore.");
                return new BusinessResult(400, "Send Notify success");
            }
            catch (Exception ex)
            {
                return new BusinessResult(500, $"Error sending notification: {ex.Message}");
            }
        }

    }
}
