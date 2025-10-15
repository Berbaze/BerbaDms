using DmsProjeckt.Data;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DmsProjeckt.Pages.Dokument
{
    public class IndexierteModel : PageModel
    {
        private readonly DokumentIndexService _service;

        public IndexierteModel(DokumentIndexService service)
        {
            _service = service;
        }

        public List<DokumentIndex> IndexDocs { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (User.IsInRole("Admin"))
                IndexDocs = await _service.GetAllIndexedAsync();
            else
                IndexDocs = await _service.GetIndexedForUserAsync(userId);
        }
    }
}
