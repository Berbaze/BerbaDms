using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace DmsProjeckt.Hubs
{
    public class SISHub : Hub
    {
        // Stocke les utilisateurs connectés à chaque document
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _documentUsers
            = new();

        public async Task JoinDocument(string documentId, string userName)
        {
            var users = _documentUsers.GetOrAdd(documentId, _ => new ConcurrentDictionary<string, string>());
            users[Context.ConnectionId] = userName;

            await Groups.AddToGroupAsync(Context.ConnectionId, documentId);
            await Clients.Group(documentId).SendAsync("UserJoined", userName);

            // 🔄 Mise à jour de la liste
            await UpdateUserList(documentId);
            Console.WriteLine($"👋 {userName} joined document {documentId}");
        }

        public async Task LeaveDocument(string documentId, string userName)
        {
            if (_documentUsers.TryGetValue(documentId, out var users))
            {
                users.TryRemove(Context.ConnectionId, out _);
                await Clients.Group(documentId).SendAsync("UserLeft", userName);
                await UpdateUserList(documentId);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, documentId);
            Console.WriteLine($"🚪 {userName} left document {documentId}");
        }

        public async Task SendChange(string documentId, string jsonData)
        {
            await Clients.OthersInGroup(documentId).SendAsync("ReceiveChange", jsonData);
            Console.WriteLine($"📡 Broadcast change for {documentId}: {jsonData}");
        }

        private async Task UpdateUserList(string documentId)
        {
            if (_documentUsers.TryGetValue(documentId, out var users))
            {
                var active = users.Values.Distinct().ToList();
                await Clients.Group(documentId).SendAsync("UpdateUserList", active);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"❌ Client {Context.ConnectionId} disconnected.");

            foreach (var kv in _documentUsers)
            {
                var documentId = kv.Key;
                var users = kv.Value;
                if (users.TryRemove(Context.ConnectionId, out var user))
                {
                    await Clients.Group(documentId).SendAsync("UserLeft", user);
                    await UpdateUserList(documentId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
