using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DmsProjeckt.Data;
namespace DmsProjeckt.Controllers
{
    [Authorize]
    [Route("Notifications")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        //private readonly UserManager<IdentityUser> _userManager;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
            //_userManager = userManager;
        }

        // GET: /Notifications/GetUserNotifications
        [HttpGet("GetUserNotifications")]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.UserNotifications
                .Include(un => un.Notification)
                .ThenInclude(n => n.NotificationType)
                .Where(un => un.UserId == userId)
                .OrderByDescending(un => un.ReceivedAt)
                .Take(20)
                .ToListAsync();

            return Json(notifications.Select(un => new
            {
                un.Id,
                un.Notification.Title,
                un.Notification.Content,
                Type = un.Notification.NotificationType?.Name,
                un.IsRead,
                receivedAt = un.ReceivedAt.ToString("g"),
                un.Notification.ActionLink
            }));
        }

        // POST: /Notifications/MarkAsRead/5
        [HttpPost("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userNotification = await _context.UserNotifications
                .FirstOrDefaultAsync(un => un.Id == id && un.UserId == userId);

            if (userNotification == null) return NotFound();
            userNotification.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userNotifs = await _context.UserNotifications
                .Where(un => un.UserId == userId && !un.IsRead)
                .ToListAsync();

            foreach (var notif in userNotifs)
                notif.IsRead = true;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: /Notifications/ToggleNotification/5

        [HttpPost]
        public async Task<IActionResult> ToggleType([FromBody] ToggleTypeDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var setting = await _context.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationTypeId == dto.TypeId);
            if (setting == null)
            {
                setting = new UserNotificationSetting
                {
                    UserId = userId,
                    NotificationTypeId = dto.TypeId,
                    Enabled = dto.Enabled
                };
                _context.UserNotificationSettings.Add(setting);
            }
            else
            {
                setting.Enabled = dto.Enabled;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetTypeSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var types = await _context.NotificationTypes.ToListAsync();
            var settings = await _context.UserNotificationSettings
                .Where(s => s.UserId == userId)
                .ToListAsync();

            var result = types.Select(t => new {
                t.Id,
                t.Name,
                t.Description,
                Enabled = settings.FirstOrDefault(s => s.NotificationTypeId == t.Id)?.Enabled ?? true
            });

            return Json(result);
        }

    }
    public class ToggleTypeDto
    {
        public int TypeId { get; set; }
        public bool Enabled { get; set; }
    }
}
