using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using DmsProjeckt.Data;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.ApplicationServices;

namespace DmsProjeckt.Pages.Dokument
{
    [IgnoreAntiforgeryToken]
    public class DashboardAdminModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly FirebaseStorageService _firebaseStorage;
        private readonly EmailService _emailService;
        public List<UserRoleViewModel> Users { get; set; }
        public List<AuditLogAdmin> Logs { get; set; } = new();
        public Dictionary<string, int> AktivitaetenProTag { get; set; } = new();
        public Dictionary<string, int> AktionenProTyp { get; set; } = new();
        public Dictionary<string, int> AktionenProBenutzer { get; set; } = new();
        public List<Abteilung> Abteilungen { get; set; } = new();
        [BindProperty] public string SelectedRole { get; set; }
        [BindProperty] public string UserId { get; set; }
        [BindProperty]
        public string NewUserId { get; set; }
        [BindProperty]
        public string FirstName { get; set; }

        [BindProperty]
        public string LastName { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }
        public DashboardAdminModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, FirebaseStorageService firebaseStorage, EmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _firebaseStorage = firebaseStorage;
            _emailService = emailService;
        }

        public async Task OnGetAsync()
        {
            var currentAdmin = await _userManager.GetUserAsync(User);

            var allUsers = _userManager.Users
                .Where(u => u.CreatedByAdminId == currentAdmin.Id)
                .ToList();

           

            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            Users = new List<UserRoleViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();
                var abteilung = _context.Abteilungen.FirstOrDefault(a => a.Id == user.AbteilungId);
                Users.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Vorname = user.Vorname,
                    Nachname = user.Nachname,
                    CurrentRole = role,
                    SelectedRole = role,
                    AvailableRoles = allRoles,
                    
                    DepartmentName = abteilung?.Name ?? "Keine Abteilung",
                    Email = user.Email
                });
            }

            // Logs & Diagramme wie gehabt
            Logs = _context.AuditLogAdmins
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            var today = DateTime.Today;
            AktivitaetenProTag = _context.AuditLogAdmins
                .Where(l => l.Timestamp >= today.AddDays(-6))
                .ToList()
                .GroupBy(l => l.Timestamp.Date)
                .ToDictionary(g => g.Key.ToString("dd.MM"), g => g.Count());

            AktionenProTyp = _context.AuditLogDokumente
                .Where(l => !string.IsNullOrEmpty(l.Aktion))
                .ToList()
                .GroupBy(l => l.Aktion)
                .ToDictionary(g => g.Key, g => g.Count());

            AktionenProBenutzer = _context.AuditLogDokumente
                .Where(l => !string.IsNullOrEmpty(l.BenutzerId))
                .ToList()
                .GroupBy(l => l.BenutzerId)
                .ToDictionary(g => g.Key, g => g.Count());

            Abteilungen = await _context.Abteilungen.ToListAsync();
        }


        public async Task<IActionResult> OnPostAsync(string? generatedPassword = null)
        {
            string passwordToUse = generatedPassword ?? Password;
            var userN = await _userManager.GetUserAsync(User);

            if (passwordToUse != ConfirmPassword && generatedPassword == null)
            {
                TempData["ErrorMessage"] = "Passwörter stimmen nicht überein!";
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                Vorname = FirstName,
                Nachname = LastName,
                FirmenName = userN.FirmenName,
                AbteilungId = DepartmentId // 🔑 Abteilungs-Id speichern
            };

            var result = await _userManager.CreateAsync(user, passwordToUse);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User {FirstName} {LastName} wurde erstellt.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }
        [BindProperty]
        public int? DepartmentId { get; set; }
        public async Task<IActionResult> OnPostCreateUserWithPermissionsAsync(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    int? DepartmentId,
    string[] Permissions)
        {
            var currentAdmin = await _userManager.GetUserAsync(User);

            if (Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "⚠️ Passwörter stimmen nicht überein!";
                return Page();
            }

            // 🔹 1. User anlegen
            var newUser = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                Vorname = FirstName,
                Nachname = LastName,
                FirmenName = currentAdmin?.FirmenName, // gleiche Firma wie Admin
                AbteilungId = DepartmentId,
                CreatedByAdminId = currentAdmin?.Id
            };
            
            var createResult = await _userManager.CreateAsync(newUser, Password);
            if (!createResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Page();
            }
            else
            {
                var roleResult = await _userManager.AddToRoleAsync(newUser, "Editor");

                if (roleResult.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User {FirstName} {LastName} wurde erstellt und hat nun die Editor-Rolle.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"User erstellt, aber Rolle konnte nicht hinzugefügt werden: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                }
            }

            // 🔹 2. Claims (Ordnerberechtigungen) setzen
            // 🔹 2. Claims (Ordnerberechtigungen) setzen
            if (Permissions != null && Permissions.Length > 0)
            {
                foreach (var perm in Permissions)
                {
                    // Claim speichern
                    await _userManager.AddClaimAsync(newUser, new Claim("FolderAccess", perm));

                    // FolderPermission speichern
                    _context.FolderPermissions.Add(new FolderPermission
                    {
                        UserId = newUser.Id,
                        FolderPath = perm,
                        GrantedByAdminId = currentAdmin.Id,
                        GrantedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync(); // wichtig!
            }


            TempData["SuccessMessage"] = $"✅ Benutzer {FirstName} {LastName} wurde mit Berechtigungen erstellt.";
            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostCreateUserAsync(string? generatedPassword = null)
        {
            string passwordToUse = generatedPassword ?? Password;
            var userN = await _userManager.GetUserAsync(User);

            if (passwordToUse != ConfirmPassword && generatedPassword == null)
            {
                TempData["ErrorMessage"] = "Passwörter stimmen nicht überein!";
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                Vorname = FirstName,
                Nachname = LastName,
                FirmenName = userN.FirmenName,
                AbteilungId = DepartmentId // 🔑 Abteilungs-Id speichern
            };

            var result = await _userManager.CreateAsync(user, passwordToUse);

            if (result.Succeeded)
            {
                // 🔥 Rolle hinzufügen
                var roleResult = await _userManager.AddToRoleAsync(user, "Editor");

                if (roleResult.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User {FirstName} {LastName} wurde erstellt und hat nun die Editor-Rolle.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"User erstellt, aber Rolle konnte nicht hinzugefügt werden: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }


        public IActionResult OnGetGeneratePassword()
        {
            // sicheres Passwort erzeugen
            var password = GenerateSecurePassword();

            // Nur den Text zurückgeben, kein HTML
            return Content(password);
        }

        // Passwortgenerator
        public static string GenerateSecurePassword(int length = 12)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@$?_-";
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return new string(bytes.Select(b => valid[b % valid.Length]).ToArray());
        }


        public async Task<IActionResult> OnGetGetFoldersAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || string.IsNullOrEmpty(currentUser.FirmenName))
                return new JsonResult(new { error = "Kein Firmenname für aktuellen Benutzer gefunden." });

            var abteilungen = await _context.Abteilungen.ToListAsync();

            var result = new List<object>();

            foreach (var dep in abteilungen)
            {
                // Pfad in Firebase (z. B. dokumente/Firma/Abteilung)
                string depPath = $"dokumente/{currentUser.FirmenName}/{dep.Name}";

                // Kategorien aus Firebase lesen
                var categories = await _firebaseStorage.ListFoldersAsync(depPath);

                result.Add(new
                {
                    id = dep.Id,
                    name = dep.Name,
                    path = depPath,
                    categories = categories.Select(c => new
                    {
                        name = c,
                        path = $"{depPath}/{c}"
                    })
                });
            }

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string UserId, string[] Permissions)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            var currentAdmin = await _userManager.GetUserAsync(User);

            // Alte Claims löschen
            var oldClaims = await _userManager.GetClaimsAsync(user);
            foreach (var c in oldClaims.Where(c => c.Type == "FolderAccess"))
                await _userManager.RemoveClaimAsync(user, c);

            // Alte FolderPermissions löschen
            var oldPermissions = _context.FolderPermissions.Where(fp => fp.UserId == user.Id);
            _context.FolderPermissions.RemoveRange(oldPermissions);

            // Neue Claims + FolderPermissions hinzufügen
            if (Permissions != null && Permissions.Length > 0)
            {
                foreach (var perm in Permissions)
                {
                    // Claim speichern
                    await _userManager.AddClaimAsync(user, new Claim("FolderAccess", perm));

                    // FolderPermission speichern
                    _context.FolderPermissions.Add(new FolderPermission
                    {
                        UserId = user.Id,
                        FolderPath = perm,
                        GrantedByAdminId = currentAdmin.Id,
                        GrantedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Berechtigungen für {user.UserName} wurden gespeichert.";
            return RedirectToPage();
        }




        public async Task<IActionResult> OnGetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new JsonResult(new { error = "User nicht gefunden" });

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var abteilungen = await _context.Abteilungen.ToListAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || string.IsNullOrEmpty(currentUser.FirmenName))
                return new JsonResult(new { error = "Kein Firmenname für Admin gefunden." });

            // 🔹 Claims laden
            var claimPermissions = claims
                .Where(c => c.Type == "FolderAccess")
                .Select(c => c.Value.ToLower().Trim())
                .ToList();

            // 🔹 DB-Einträge laden
            var dbPermissions = await _context.FolderPermissions
                .Where(fp => fp.UserId == user.Id)
                .Select(fp => fp.FolderPath.ToLower().Trim())
                .ToListAsync();

            // 🔹 Beides zusammenführen
            var allPermissions = claimPermissions
                .Union(dbPermissions) // Doppelte vermeiden
                .ToList();

            // Alle Abteilungen aus der DB holen
            var folderTree = abteilungen.Select(dep => new FolderVm
            {
                Id = dep.Id,
                Name = dep.Name,
                Path = $"dokumente/{currentUser.FirmenName}/{dep.Name}".ToLower(),
                Checked = allPermissions.Contains($"dokumente/{currentUser.FirmenName.ToLower()}/{dep.Name.ToLower()}/*"),
                Categories = new List<object>()
            }).ToList();

            // Claims/Permissions, die nicht mehr in Abteilungen existieren → als "verwaist" anzeigen
            foreach (var perm in allPermissions)
            {
                if (!folderTree.Any(f => f.Path == perm.Replace("/*", "")))
                {
                    folderTree.Add(new FolderVm
                    {
                        Id = -1,
                        Name = $"(verwaist) {perm}",
                        Path = perm.Replace("/*", ""),
                        Checked = true
                    });
                }
            }

            return new JsonResult(new
            {
                id = user.Id,
                name = user.UserName,
                vorname = user.Vorname,
                nachname = user.Nachname,
                email = user.Email,
                abteilung = _context.Abteilungen.FirstOrDefault(a => a.Id == user.AbteilungId)?.Name,
                abteilungId = user.AbteilungId,
                role = roles.FirstOrDefault() ?? "Kein",
                permissions = allPermissions,
                folders = folderTree
            });
        }


        public async Task<IActionResult> OnGetFolderPermissionsAsync(string folderPath)
        {
            try
            {
                var normalized = NormalizePath(folderPath);

                var allPermissions = await _context.FolderPermissions
                    .Include(fp => fp.User)
                    .Include(fp => fp.GrantedByAdmin)
                    .ToListAsync();
                foreach (var fp in allPermissions)
                {
                    Console.WriteLine($"DB: {fp.FolderPath} | Normalized: {NormalizePath(fp.FolderPath)}");
                }

                var permissions = allPermissions
                    .Where(fp =>
                        NormalizePath(fp.FolderPath) == normalized ||
                        NormalizePath(fp.FolderPath) == normalized + "/*" ||
                        NormalizePath(fp.FolderPath).StartsWith(normalized + "/"))
                    .ToList();

                var result = permissions.Select(fp => new
                {
                    id = fp.User.Id,
                    userName = fp.User.UserName,
                    vorname = fp.User.Vorname,
                    nachname = fp.User.Nachname,
                    profilbildUrl = fp.User.ProfilbildUrl,
                    grantedBy = $"{fp.GrantedByAdmin?.Vorname} {fp.GrantedByAdmin?.Nachname}",
                    grantedAt = fp.GrantedAt.ToString("dd.MM.yyyy HH:mm")
                });

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }




        public async Task<IActionResult> OnPostUpdateUserAndPermissionsAsync(string UserId, string FirstName, string LastName, string Email, int DepartmentId, string[] Permissions)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            // User-Daten aktualisieren
            user.Vorname = FirstName;
            user.Nachname = LastName;
            user.Email = Email;
            user.UserName = Email; // wichtig für Login
            user.AbteilungId = DepartmentId;

            await _userManager.UpdateAsync(user);

            // Alte Claims löschen
            var oldClaims = await _userManager.GetClaimsAsync(user);
            foreach (var c in oldClaims.Where(c => c.Type == "FolderAccess"))
                await _userManager.RemoveClaimAsync(user, c);

            // Neue Claims speichern
            if (Permissions != null && Permissions.Length > 0)
            {
                foreach (var perm in Permissions)
                    await _userManager.AddClaimAsync(user, new Claim("FolderAccess", perm));
            }

            TempData["SuccessMessage"] = $"✅ Benutzer {user.Vorname} {user.Nachname} wurde aktualisiert.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync([FromBody] DeleteUserRequest request)
        {
            if (string.IsNullOrEmpty(request?.UserId))
                return new JsonResult(new { success = false, message = "❌ Kein Benutzer angegeben." });

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return new JsonResult(new { success = false, message = "❌ Benutzer nicht gefunden." });

            try
            {
                // 🔹 1. Claims löschen
                var claims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in claims.Where(c => c.Type == "FolderAccess"))
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }

                // 🔹 2. FolderPermissions löschen
                var folderPerms = _context.FolderPermissions.Where(fp => fp.UserId == user.Id);
                _context.FolderPermissions.RemoveRange(folderPerms);

                // 🔹 3. Kunden-Referenzen löschen (falls noch vorhanden)
                

                await _context.SaveChangesAsync();

                // 🔹 4. User löschen
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                    return new JsonResult(new { success = true, message = $"✅ Benutzer {user.UserName} wurde gelöscht." });

                return new JsonResult(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"❌ Exception: {ex.Message}" });
            }
        }



        public async Task<IActionResult> OnGetGetUsers()
        {
            var currentAdmin = await _userManager.GetUserAsync(User);

            var allUsers = _userManager.Users
                .Where(u => u.CreatedByAdminId == currentAdmin.Id)
                .ToList();

            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            var users = new List<object>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();
                var abteilung = _context.Abteilungen.FirstOrDefault(a => a.Id == user.AbteilungId);

                users.Add(new
                {
                    userId = user.Id,
                    userName = user.UserName,
                    departmentName = abteilung?.Name ?? "Keine Abteilung",
                    email = user.Email
                });
            }

            return new JsonResult(users);
        }


        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            return path
                .Trim()
                .Replace("\u00A0", " ") // non-breaking space
                .Replace("//", "/")     // doppelte Slashes
                .ToLowerInvariant();
        }
        public async Task<IActionResult> OnPostResetPasswordAsync([FromBody] DeleteUserRequest request)
        {
            if (string.IsNullOrEmpty(request?.UserId))
                return new JsonResult(new { success = false, message = "❌ Kein Benutzer angegeben." });

            Console.WriteLine($"DEBUG ResetPassword UserId: {request.UserId}");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return new JsonResult(new { success = false, message = "❌ Benutzer nicht gefunden." });

            // Neues Passwort generieren
            var newPassword = GenerateSecurePassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!resetResult.Succeeded)
                return new JsonResult(new { success = false, message = string.Join(", ", resetResult.Errors.Select(e => e.Description)) });

            // Hier z. B. Mail-Service nutzen, um das Passwort per Mail zu senden
            // await _emailService.SendPasswordResetAsync(user.Email, newPassword);
            await _emailService.SendEmailAsync(user.Email, "Passwort zurückgesetzt",
               $"Hallo {user.UserName},\n\nDein neues Passwort lautet: {newPassword}\nBitte ändere es nach dem Login.");

            return new JsonResult(new { success = true, message = $"✅ Neues Passwort für {user.UserName} wurde gesetzt und per Mail verschickt." });
        }

        


    }
    public class DeleteUserRequest
    {
        public string UserId { get; set; }
    }
    public class RecentDocVm
    {
        public string Dateiname { get; set; }
        public string UserId { get; set; }
        public DateTime HochgeladenAm { get; set; }
    }

    public class RecentActivityVm
    {
        public string BenutzerId { get; set; }
        public string Aktion { get; set; }
        public DateTime Zeitstempel { get; set; }
    }
    public class FolderVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Checked { get; set; }
        public List<object> Categories { get; set; }
    }

    public class CategoryVm
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
