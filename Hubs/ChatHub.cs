using DmsProjeckt.Data;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
namespace DmsProjeckt.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        public ChatHub(ApplicationDbContext db) { _db = db; }

        public async Task SendGroupMessage(int groupId, string message)
        {
            var senderId = Context.UserIdentifier;
            var senderName = Context.User.Identity.Name;
            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                SenderName = senderName,
                GroupId = groupId,
                Message = message,
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(chatMsg);
            await _db.SaveChangesAsync();
            await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", groupId, senderName, message, chatMsg.SentAt);
        }

        public async Task SendPrivateMessage(string toUserId, string message)
        {
            var senderId = Context.UserIdentifier;
            var senderName = Context.User.Identity.Name;
            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                SenderName = senderName,
                ReceiverId = toUserId,
                Message = message,
                SentAt = DateTime.UtcNow
            };
            _db.ChatMessages.Add(chatMsg);
            await _db.SaveChangesAsync();
            await Clients.User(toUserId).SendAsync("ReceivePrivateMessage", senderId, senderName, message, chatMsg.SentAt);
            await Clients.User(senderId).SendAsync("ReceivePrivateMessage", toUserId, senderName, message, chatMsg.SentAt);
        }
        public async Task NotifyMessageRead(int messageId, string readerId, string readerName)
        {
            await Clients.All.SendAsync("MessageRead", new
            {
                MessageId = messageId,
                ReaderId = readerId,
                ReaderName = readerName,
                ReadAt = DateTime.UtcNow
            });
        }

        public async Task JoinGroup(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        }
    
}
}
