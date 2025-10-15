using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DmsProjeckt.Pages;
[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;

    private readonly UserManager<ApplicationUser> _userManager;
    public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;

        _userManager = userManager;
    }
    public void OnGet()
    {

    }
}
