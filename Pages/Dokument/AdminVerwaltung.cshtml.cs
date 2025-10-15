using System.Security.Claims;
using DmsProjeckt.Controllers;
using DmsProjeckt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace DmsProjeckt.Pages.Dokument
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminVerwaltungModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public List<AuditLogAdmin> Logs { get; set; } = new();

        public AdminVerwaltungModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [BindProperty]
        public string NewUserId { get; set; }
        [BindProperty]
        public string SelectedRole { get; set; }
        [BindProperty]
        public string UserId { get; set; }

        public List<UserRoleViewModel> Users { get; set; }

        public async Task OnGetAsync()
        {
            var allUsers = _userManager.Users.ToList();
            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            TempData["SuccessMessage"] = "Rolle wurde erfolgreich aktualisiert.";

            Users = new List<UserRoleViewModel>(); // 🔧 CORRECTION ICI

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Aucun";

                Users.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    CurrentRole = role,
                    SelectedRole = role,
                    AvailableRoles = allRoles
                });
            }

            // ➕ Charger tous les logs admin
            Logs = _context.AuditLogAdmins
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            var currentRoles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, SelectedRole);

            // 📝 Audit-Log auf Deutsch hinzufügen
            _context.AuditLogAdmins.Add(new AuditLogAdmin
            {
                AdminId = _userManager.GetUserId(User),
                TargetUserId = user.Id,
                Action = $"Die Rolle des Benutzers {user.Email} wurde zu {SelectedRole} geändert.",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rolle wurde erfolgreich aktualisiert.";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostCreateUserAsync(string Username, string Email, string Password)
        {
            var user = new ApplicationUser { UserName = Username, Email = Email };
            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User {Username} angelegt!";
                // user.Id speichern für nächsten Schritt
                NewUserId = user.Id;
            }
            else
            {
                TempData["SuccessMessage"] = "Fehler beim Erstellen des Benutzers.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string UserId, string Role, string[] Permissions)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(Role));
            }

            await _userManager.AddToRoleAsync(user, Role);

            foreach (var perm in Permissions)
            {
                await _userManager.AddClaimAsync(user, new Claim("FolderAccess", perm));
            }

            TempData["SuccessMessage"] = $"Benutzerrechte für {user.UserName} gesetzt!";
            return RedirectToPage();
        }

    }

}
