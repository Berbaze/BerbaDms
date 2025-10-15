// Licensed to the .NET Foundation under one or more agreements.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;
using DmsProjeckt.Service;

namespace DmsProjeckt.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;


        public RegisterModel(
     UserManager<ApplicationUser> userManager,
     IUserStore<ApplicationUser> userStore,
     SignInManager<ApplicationUser> signInManager,
     ILogger<RegisterModel> logger,
     IEmailSender emailSender,
     ApplicationDbContext context
    ) // <-- 🔥 HIER
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            // <-- 🔥 HIER
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "E-Mail")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Vorname")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Nachname")]
            public string LastName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Passwort")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Passwort bestätigen")]
            [Compare("Password", ErrorMessage = "Passwörter stimmen nicht überein.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Firmenname")]
            public string FirmenName { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    Vorname = Input.FirstName,
                    Nachname = Input.LastName,
                    FirmenName = Input.FirmenName
                };

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // 👑 Adminrolle automatisch vergeben
                    if (!await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        var roleStore = _context.Roles.FirstOrDefault(r => r.Name == "Admin");
                        if (roleStore == null)
                        {
                            // Falls die Rolle noch nicht existiert → erstellen
                            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
                            if (!roleResult.Succeeded)
                            {
                                foreach (var error in roleResult.Errors)
                                    ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                        else
                        {
                            await _userManager.AddToRoleAsync(user, "Admin");
                        }
                    }

                    _logger.LogInformation("👑 Neuer User wurde zur Adminrolle hinzugefügt.");

                    _logger.LogInformation("✅ User created a new account with password.");

                    // 🧠 Ajouter FK directement
                    var neuerKunde = new Kunden
                    {
                        Name = Input.LastName,
                        Vorname = Input.FirstName,
                        Adresse = "Unbekannt",
                        Email = Input.Email,
                        ApplicationUserId = user.Id,
                        FirmenName = user.FirmenName// 💥 ici direct
                    };

                    _context.Kunden.Add(neuerKunde);
                    await _context.SaveChangesAsync(); // Id + FK sont prêts

                    // 🔗 Join table
                    var kundenBenutzer = new KundeBenutzer
                    {
                        KundenId = neuerKunde.Id,
                        ApplicationUserId = user.Id
                    };
                    _context.KundeBenutzer.Add(kundenBenutzer);
                    await _context.SaveChangesAsync();

                    // 📧 Email confirmation
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        null,
                        new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Bitte bestätige deinen Account durch <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Klick hier</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }



        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("UserStore ohne E-Mail-Support wird nicht unterstützt.");

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
