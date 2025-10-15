using DmsProjeckt.Data;
using Microsoft.AspNetCore.Mvc;

namespace DmsProjeckt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var items = _context.DashboardItem.ToList();
            return View();
        }
    }
}
