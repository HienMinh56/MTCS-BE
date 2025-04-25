using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace MTCS.Service.Services
{
    public interface IChatService
    {
        Task SaveMessageAsync(string driverId, string staffId, string senderId, string content);
        Task MarkMessageAsReadAsync(string driverId, string staffId, string messageId);
    }
    public class ChatService : IChatService
    {
        private readonly FirestoreDb _firestoreDb;

        public ChatService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task SaveMessageAsync(string driverId, string staffId, string senderId, string content)
        {
            var chatId = $"{driverId}_{staffId}";
            var chatRef = _firestoreDb.Collection("chats").Document(chatId);

            var message = new Dictionary<string, object>
        {
            { "senderId", senderId },
            { "content", content },
            { "sentAt", Timestamp.GetCurrentTimestamp() },  // Thêm thời gian gửi
            { "readAt", null } // Thời gian xem, lúc đầu là null
        };

            // Lưu tin nhắn vào subcollection "messages"
            await chatRef.Collection("messages").AddAsync(message);

            // Cập nhật metadata của cuộc trò chuyện
            var chatMeta = new Dictionary<string, object>
        {
            { "participants", new[] { driverId, staffId } },
            { "lastMessage", content },
            { "timestamp", Timestamp.GetCurrentTimestamp() }
        };

            await chatRef.SetAsync(chatMeta, SetOptions.MergeAll);
        }

        // Hàm cập nhật thời gian xem tin nhắn
        public async Task MarkMessageAsReadAsync(string driverId, string staffId, string messageId)
        {
            var chatId = $"{driverId}_{staffId}";
            var messageRef = _firestoreDb.Collection("chats")
                                          .Document(chatId)
                                          .Collection("messages")
                                          .Document(messageId);

            // Cập nhật trường readAt khi người dùng xem tin nhắn
            await messageRef.UpdateAsync("readAt", Timestamp.GetCurrentTimestamp());
        }
    }
}
