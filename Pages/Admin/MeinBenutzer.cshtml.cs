using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DmsProjeckt.Pages.Admin
{
    
    public class MeinBenutzerModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AdminAuditService _auditService;
        private readonly ApplicationDbContext _context; // Assuming you have a DbContext for Audit logs
        private readonly RoleManager<IdentityRole> _roleManager;

        public List<AuditLogAdmin> Logs { get; set; } = new();
        public MeinBenutzerModel(UserManager<ApplicationUser> userManager, AdminAuditService auditService, ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _auditService = auditService;
            _context = context;
            _roleManager = roleManager;
        }

        public List<UserWithRoleViewModel> Users { get; set; } = new();
        [BindProperty] public string UserId { get; set; }
        [BindProperty] public string SelectedRole { get; set; }
        public List<string> AvailableRoles { get; set; } = new();
        public async Task OnGetAsync()
        {
            var currentAdminId = _userManager.GetUserId(User);

            // Charger les r�les disponibles
            AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            // Obtenir tous les utilisateurs cr��s par l'admin actuel
            var createdUsers = _userManager.Users
                .Where(u => u.CreatedByAdminId == currentAdminId)
                .ToList();

            // Pr�parer la liste des utilisateurs avec leurs r�les
            Users = new List<UserWithRoleViewModel>();
            foreach (var user in createdUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Users.Add(new UserWithRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault()
                });
            }

            // R�cup�rer uniquement les logs que moi (admin connect�) j�ai g�n�r�s
            var createdUserIds = createdUsers.Select(u => u.Id).ToList();

            Logs = _context.AuditLogAdmins
                .Where(l => createdUserIds.Contains(l.TargetUserId) && l.AdminId == currentAdminId)
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        }



        public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string selectedRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(selectedRole))
            {
                TempData["ErrorMessage"] = "Benutzer-ID oder Rolle fehlt.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Benutzer nicht gefunden.";
                return RedirectToPage();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, selectedRole);

            // Enregistre l�audit log
            _context.AuditLogAdmins.Add(new AuditLogAdmin
            {
                AdminId = _userManager.GetUserId(User),
                TargetUserId = user.Id,
                Action = $"Die Rolle des Benutzers {user.Email} wurde zu {selectedRole} ge�ndert.",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rolle erfolgreich ge�ndert.";
            return RedirectToPage();
        }



        public class UserWithRoleViewModel
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string? Role { get; set; }
        }
    }
}
