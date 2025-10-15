using DmsProjeckt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Admin
{
    

    public class CreateUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public CreateUserModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Password { get; set; }
        [BindProperty] public string SelectedRole { get; set; }
        [BindProperty] public string Vorname { get; set; }
        [BindProperty] public string Nachname { get; set; }
        [BindProperty] public string FirmenName { get; set; }
        [BindProperty] public int AbteilungId { get; set; }   // 🔑 FK vers Abteilung

        public List<Abteilung> Abteilungen { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SuccessMessage { get; set; }

        // Charger les abteilungen dans le formulaire
        public async Task OnGetAsync()
        {
            // ✅ si l’admin ne peut gérer QUE son Abteilung :
            var admin = await _userManager.GetUserAsync(User);
            if (admin?.AbteilungId != null)
            {
                Abteilungen = await _context.Abteilungen
                    .Where(a => a.Id == admin.AbteilungId)
                    .ToListAsync();
            }
            else
            {
                Abteilungen = await _context.Abteilungen.ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // recharger dropdown en cas d'erreur
                return Page();
            }

            var creatorId = _userManager.GetUserId(User);

            // Créer l’utilisateur
            var user = new ApplicationUser
            {
                UserName = Email,
                Email = Email,
                CreatedByAdminId = creatorId,
                Vorname = Vorname,
                Nachname = Nachname,
                FirmenName = FirmenName,
                AbteilungId = AbteilungId // ✅ assignation
            };

            IdentityResult result;
            var dbUser = await _userManager.FindByEmailAsync(Email);

            if (dbUser == null)
            {
                result = await _userManager.CreateAsync(user, Password);
                dbUser = user;
            }
            else
            {
                result = IdentityResult.Success;
            }

            if (result.Succeeded)
            {
                // ✅ Ajouter le rôle
                if (!await _userManager.IsInRoleAsync(dbUser, SelectedRole))
                {
                    if (!await _roleManager.RoleExistsAsync(SelectedRole))
                        await _roleManager.CreateAsync(new IdentityRole(SelectedRole));

                    await _userManager.AddToRoleAsync(dbUser, SelectedRole);
                }

                // --- KUNDEN-LOGIK ---
                var kunde = _context.Kunden.FirstOrDefault(k => k.Name == FirmenName);
                if (kunde == null)
                {
                    kunde = new Kunden
                    {
                        FirmenName = FirmenName,
                        Vorname = Vorname,
                        Name = Nachname,
                        Adresse = "Unbekannt",
                        Email = Email,
                        ApplicationUserId = dbUser.Id
                    };
                    _context.Kunden.Add(kunde);
                    await _context.SaveChangesAsync();
                }

                if (!_context.KundeBenutzer.Any(kb => kb.ApplicationUserId == dbUser.Id && kb.KundenId == kunde.Id))
                {
                    var kb = new KundeBenutzer
                    {
                        ApplicationUserId = dbUser.Id,
                        KundenId = kunde.Id
                    };
                    _context.KundeBenutzer.Add(kb);
                    await _context.SaveChangesAsync();
                }

                SuccessMessage = $"Benutzer {Email} wurde erfolgreich als {SelectedRole} erstellt und der Abteilung zugeordnet.";
                await OnGetAsync(); // recharger les abteilungen
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await OnGetAsync();
            return Page();
        }
    }
}
