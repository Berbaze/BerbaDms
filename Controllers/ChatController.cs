using System.Text.Json.Serialization;
using DmsProjeckt.Data;
using DmsProjeckt.Hubs;
using DmsProjeckt.Service;
using Firebase.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
namespace DmsProjeckt.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FirebaseStorageService _firebaseStorageService;
        private readonly IHubContext<ChatHub> _hubContext;
        public ChatController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, FirebaseStorageService firebaseStorage, IHubContext<ChatHub> hubContext)
        {
            _db = db;
            _userManager = userManager;
            _firebaseStorageService = firebaseStorage;
            _hubContext = hubContext;
        }

        // 1. Alle Chats des Users (Gruppen und private)
        [HttpGet("userchats")]
        public async Task<IActionResult> GetUserChats()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { error = "User nicht gefunden oder nicht eingeloggt" });
            }

            try
            {
                var allUsers = await _db.Users.ToListAsync();

                // --- Gruppenchats laden ---
                var groupChatsRaw = await _db.ChatGroupMembers
                    .Where(cgm => cgm.UserId == user.Id)
                    .Select(cgm => new
                    {
                        cgm.ChatGroupId,
                        cgm.ChatGroup.Name,
                        cgm.ChatGroup.AvatarUrl
                    })
                    .ToListAsync();

                var groupChats = groupChatsRaw
                    .Select(gc => new ChatViewModel
                    {
                        ChatId = gc.ChatGroupId.ToString(),
                        Type = "group",
                        DisplayName = gc.Name,
                        AvatarUrl = string.IsNullOrEmpty(gc.AvatarUrl) ? "/images/group-icon.png" : gc.AvatarUrl,
                        LastMessageTime = _db.ChatMessages
                            .Where(m => m.GroupId == gc.ChatGroupId)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => (DateTime?)m.SentAt)
                            .FirstOrDefault(),
                        UnreadCount = _db.ChatMessages
                            .Where(m => m.GroupId == gc.ChatGroupId && m.SenderId != user.Id)
                            .AsEnumerable() // 👉 Rest im Speicher auswerten
                            .Count(m => !_db.MessageRead.Any(r => r.MessageId == m.Id && r.UserId == user.Id))
                    })
                    .ToList();

                // --- Private Chats laden ---
                var privateChatsRaw = await _db.ChatMessages
                    .Where(m => (m.SenderId == user.Id && m.ReceiverId != null) ||
                                (m.ReceiverId == user.Id && m.SenderId != null))
                    .Select(m => new
                    {
                        ChatPartnerId = m.SenderId == user.Id ? m.ReceiverId : m.SenderId,
                        m.SentAt,
                        m.Id,
                        m.SenderId,
                        m.ReceiverId
                    })
                    .Where(x => x.ChatPartnerId != null)
                    .ToListAsync();

                var privateChats = privateChatsRaw
                    .GroupBy(x => x.ChatPartnerId)
                    .Select(g => new ChatViewModel
                    {
                        ChatId = g.Key,
                        Type = "private",
                        DisplayName = _db.Users
                            .Where(u => u.Id == g.Key)
                            .Select(u => (u.Vorname + " " + u.Nachname).Trim())
                            .FirstOrDefault(),
                        AvatarUrl = _db.Users
                            .Where(u => u.Id == g.Key)
                            .Select(u => u.ProfilbildUrl)
                            .FirstOrDefault(),
                        LastMessageTime = g.OrderByDescending(m => m.SentAt)
                                           .Select(m => (DateTime?)m.SentAt)
                                           .FirstOrDefault(),
                        UnreadCount = g
                            .Where(m => m.SenderId != user.Id)
                            .AsEnumerable() // 👉 verhindert Aggregate-Fehler
                            .Count(m => !_db.MessageRead.Any(r => r.MessageId == m.Id && r.UserId == user.Id))
                    })
                    .ToList();

                // --- Alle Chats zusammen ---
                var allChats = groupChats.Concat(privateChats)
                    .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                    .ToList();

                return Ok(new
                {
                    UserChats = allChats,
                    AllUsers = allUsers.Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.ProfilbildUrl,
                        u.Vorname,
                        u.Nachname
                    }),
                    CurrentUserId = user.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }




        // 2. Nachrichten für einen Chat abrufen
        [HttpGet("messages")]
        public async Task<IActionResult> GetChatMessages([FromQuery] string chatId, [FromQuery] string type)
        {
            var user = await _userManager.GetUserAsync(User);
            List<ChatMessage> chatMessages = new();
            string selectedChatName = null;
            string avatarUrl = null;  // <-- Avatar vorbereiten

            if (string.IsNullOrEmpty(chatId) || string.IsNullOrEmpty(type))
                return BadRequest("chatId und type sind erforderlich");

            if (type == "group")
            {
                if (!int.TryParse(chatId, out int groupId))
                    return BadRequest("Ungültige Gruppen-ID");

                chatMessages = await _db.ChatMessages
                    .Where(m => m.GroupId == groupId)
                    .OrderBy(m => m.SentAt)
                    .Take(100)
                    .ToListAsync();

                selectedChatName = await _db.ChatGroups
                    .Where(g => g.Id == groupId)
                    .Select(g => g.Name)
                    .FirstOrDefaultAsync();

                avatarUrl = await _db.ChatGroups
                    .Where(g => g.Id == groupId)
                    .Select(g => g.AvatarUrl)
                    .FirstOrDefaultAsync();
            }
            else if (type == "private")
            {
                chatMessages = await _db.ChatMessages
                    .Where(m => (m.SenderId == user.Id && m.ReceiverId == chatId) ||
                                (m.ReceiverId == user.Id && m.SenderId == chatId))
                    .OrderBy(m => m.SentAt)
                    .Take(100)
                    .ToListAsync();

                selectedChatName = await _db.Users
                    .Where(u => u.Id == chatId)
                    .Select(u => u.Vorname + " " + u.Nachname)
                    .FirstOrDefaultAsync();

                avatarUrl = await _db.Users
                    .Where(u => u.Id == chatId)
                    .Select(u => u.ProfilbildUrl)
                    .FirstOrDefaultAsync();
            }
            else
            {
                return BadRequest("Ungültiger Typ");
            }

            return Ok(new
            {
                ChatMessages = chatMessages,
                SelectedChatName = selectedChatName,
                AvatarUrl = string.IsNullOrEmpty(avatarUrl)
                    ? (type == "group" ? "/images/group-icon.png" : "/images/default-profile.png")
                    : avatarUrl
            });
        }

        // 3. Gruppe erstellen
        [HttpPost("creategroup")]
        public async Task<IActionResult> CreateGroup([FromForm] CreateGroupRequest dto, IFormFile? avatar)
        {
            if (dto == null)
                return BadRequest("Ungültiges JSON.");

            if (string.IsNullOrWhiteSpace(dto.GroupName) || dto.UserIds == null || !dto.UserIds.Any())
                return BadRequest("Gruppenname und mindestens ein Mitglied sind erforderlich.");

            var members = await _db.Users.Where(u => dto.UserIds.Contains(u.Id)).ToListAsync();
            var user = await _userManager.GetUserAsync(User);

            if (!members.Any() || user == null)
                return BadRequest("Ungültige Mitglieder");

            // Gruppe anlegen
            var group = new ChatGroup
            {
                Name = dto.GroupName
            };

            // Falls Bild hochgeladen wurde → in Firebase speichern
            if (avatar != null && avatar.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(avatar.ContentType))
                    return BadRequest("Nur JPG, PNG oder GIF erlaubt.");

                var objectName = $"gruppen/{Guid.NewGuid()}_{Path.GetFileName(avatar.FileName)}";
                using var stream = avatar.OpenReadStream();
                await _firebaseStorageService.UploadAsync(stream, objectName);

                var imageUrl = $"https://storage.googleapis.com/{_firebaseStorageService.Bucket}/{objectName}";
                group.AvatarUrl = imageUrl; // <-- Neue Spalte in ChatGroup Tabelle!
            }
            else
            {
                group.AvatarUrl = "/images/group-icon.png"; // default
            }

            _db.ChatGroups.Add(group);
            await _db.SaveChangesAsync();

            // Alle Mitglieder + Ersteller hinzufügen
            var allMemberIds = new HashSet<string>(dto.UserIds) { user.Id };
            foreach (var memberId in allMemberIds)
            {
                _db.ChatGroupMembers.Add(new ChatGroupMember
                {
                    ChatGroupId = group.Id,
                    UserId = memberId
                });
            }
            await _db.SaveChangesAsync();

            return Ok(new { chatId = group.Id, avatarUrl = group.AvatarUrl });
        }
        // --- Gruppendetails abrufen ---
        [HttpGet("groupdetails")]
        public async Task<IActionResult> GetGroupDetails([FromQuery] int groupId)
        {
            var group = await _db.ChatGroups
                .Include(g => g.ChatGroupMembers)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null) return NotFound();

            return Ok(new
            {
                name = group.Name,
                avatarUrl = string.IsNullOrEmpty(group.AvatarUrl) ? "/images/group-icon.png" : group.AvatarUrl,
                members = group.ChatGroupMembers.Select(m => new {
                    m.User.Id,
                    name = (m.User.Vorname + " " + m.User.Nachname).Trim(),
                    avatarUrl = string.IsNullOrEmpty(m.User.ProfilbildUrl) ? "/images/default-profile.png" : m.User.ProfilbildUrl
                })
            });
        }

        // --- Gruppenname/Avatar ändern ---
        [HttpPost("updategroup")]
        public async Task<IActionResult> UpdateGroup([FromForm] int groupId, [FromForm] string? name, IFormFile? avatar)
        {
            var group = await _db.ChatGroups.FindAsync(groupId);
            if (group == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(name))
                group.Name = name;

            if (avatar != null && avatar.Length > 0)
            {
                var objectName = $"gruppen/{Guid.NewGuid()}_{Path.GetFileName(avatar.FileName)}";
                using var stream = avatar.OpenReadStream();
                await _firebaseStorageService.UploadAsync(stream, objectName);
                group.AvatarUrl = $"https://storage.googleapis.com/{_firebaseStorageService.Bucket}/{objectName}";
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, name = group.Name, avatarUrl = group.AvatarUrl });
        }

        // --- Mitglied hinzufügen ---
        [HttpPost("addmember")]
        public async Task<IActionResult> AddMember([FromForm] int groupId, [FromForm] string userId)
        {
            if (await _db.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == groupId && m.UserId == userId))
                return BadRequest("User ist schon Mitglied.");

            _db.ChatGroupMembers.Add(new ChatGroupMember { ChatGroupId = groupId, UserId = userId });
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // --- Gruppe verlassen ---
        [HttpPost("leavegroup")]
        public async Task<IActionResult> LeaveGroup([FromForm] int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _db.ChatGroupMembers.FirstOrDefaultAsync(m => m.ChatGroupId == groupId && m.UserId == user.Id);

            if (member == null) return BadRequest("Du bist kein Mitglied.");

            _db.ChatGroupMembers.Remove(member);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }
        [HttpPost("markasread")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkReadDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            IQueryable<ChatMessage> query;

            if (dto.Type == "group")
            {
                if (!int.TryParse(dto.ChatId, out int groupId))
                    return BadRequest("Ungültige Gruppen-ID");

                query = _db.ChatMessages.Where(m => m.GroupId == groupId && m.SenderId != user.Id);
            }
            else if (dto.Type == "private")
            {
                query = _db.ChatMessages.Where(m =>
                    (m.SenderId == dto.ChatId && m.ReceiverId == user.Id) ||
                    (m.ReceiverId == dto.ChatId && m.SenderId == user.Id));
            }
            else
            {
                return BadRequest("Ungültiger Chat-Typ");
            }

            var messages = await query.ToListAsync();
            var newlyRead = new List<ChatMessage>();

            foreach (var msg in messages)
            {
                if (!_db.MessageRead.Any(r => r.MessageId == msg.Id && r.UserId == user.Id))
                {
                    _db.MessageRead.Add(new MessageRead
                    {
                        MessageId = msg.Id,
                        UserId = user.Id,
                        ReadAt = DateTime.UtcNow
                    });

                    newlyRead.Add(msg);

                    // ✅ SignalR: Sender benachrichtigen
                    await _hubContext.Clients.User(msg.SenderId).SendAsync("MessageRead", new
                    {
                        MessageId = msg.Id,
                        ChatId = dto.ChatId,
                        Type = dto.Type,
                        ReaderId = user.Id,
                        ReaderName = $"{user.Vorname} {user.Nachname}",
                        ReadAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();

            return Ok(new { success = true, count = newlyRead.Count });
        }




        // --- DTOs / ViewModels ---
        public class CreateGroupRequest
        {
            [JsonPropertyName("groupName")]
            public string GroupName { get; set; }

            [JsonPropertyName("userIds")]
            public List<string> UserIds { get; set; }
        }

        public class ChatViewModel
        {
            public string ChatId { get; set; }
            public string Type { get; set; } // "group" oder "private"
            public string DisplayName { get; set; }
            public string AvatarUrl { get; set; }

            public DateTime? LastMessageTime { get; set; }
            public int UnreadCount { get; set; }
        }
        public class MarkReadDto
        {
            public string ChatId { get; set; }
            public string Type { get; set; } // "private" oder "group"
        }

    }
}
