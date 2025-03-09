using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace MTCS.Service
{
    public interface IFCMService
    {
        FirebaseMessaging GetMessagingClient();
        FirestoreDb GetFirestoreDb();
    }

    public class FCMService : IFCMService
    {
        private readonly FirebaseMessaging _firebaseMessaging;
        private readonly FirestoreDb _firestoreDb;

        public FCMService(IConfiguration configuration, ILogger<FCMService> logger)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                logger.LogInformation($"Current Environment: {environment}");

                GoogleCredential googleCredential;

                if (environment == "Production")
                {
                    var base64JsonAuth = Environment.GetEnvironmentVariable("FCM_CREDENTIALS");

                    if (string.IsNullOrEmpty(base64JsonAuth))
                    {
                        throw new InvalidOperationException("🔥 FCM_CREDENTIALS environment variable is missing.");
                    }

                    var jsonAuthBytes = Convert.FromBase64String(base64JsonAuth);
                    var jsonAuth = System.Text.Encoding.UTF8.GetString(jsonAuthBytes);
                    googleCredential = GoogleCredential.FromJson(jsonAuth);
                }
                else
                {
                    var firebaseAuthPath = configuration["FirebaseFCM:AuthFile"];

                    if (!File.Exists(firebaseAuthPath))
                    {
                        throw new FileNotFoundException($"🔥 Firebase FCM Auth file not found: {firebaseAuthPath}");
                    }

                    googleCredential = GoogleCredential.FromFile(firebaseAuthPath);
                }

                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = googleCredential
                    });
                }

                _firebaseMessaging = FirebaseMessaging.DefaultInstance;
                _firestoreDb = new FirestoreDbBuilder
                {
                    ProjectId = configuration["FirebaseFCM:ProjectId"],
                    Credential = googleCredential
                }.Build();

            }
            catch (Exception ex)
            {
                logger.LogError($"🔥 Error initializing FCMService: {ex.Message}");
                throw;
            }
        }

        public FirebaseMessaging GetMessagingClient() => _firebaseMessaging;

        public FirestoreDb GetFirestoreDb() => _firestoreDb;
    }
}
