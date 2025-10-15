using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DmsProjeckt.Pages.Pdf
{
    public class EditOneModel : PageModel
    {
        private readonly FirebaseStorageService _firebaseStorageService;
        private readonly PdfSigningService _pdfSigningService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EditOneModel> _logger;

        public EditOneModel(
            FirebaseStorageService firebaseStorageService,
            PdfSigningService pdfSigningService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ILogger<EditOneModel> logger)
        {
            _firebaseStorageService = firebaseStorageService;
            _pdfSigningService = pdfSigningService;
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string FileName { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string Source { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string FilePath { get; set; } = string.Empty;

        public string FirmaName { get; set; } = string.Empty;

        public string SasUrl { get; set; } = string.Empty;
        [BindProperty(SupportsGet = true)]
        public string OriginalPath { get; set; } = string.Empty;
        public List<Dokumente> Dokumente { get; set; } = new();

        public string CleanedPath { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                _logger.LogWarning("Aucun FileName reçu dans EditOne !");
                return BadRequest("Paramètre 'FileName' manquant.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            FirmaName = !string.IsNullOrWhiteSpace(user.FirmenName) ? user.FirmenName : "Ma Société";

            // ✅ Nettoyer FileName
            FileName = Uri.UnescapeDataString(FileName ?? "").Trim();

            if (FileName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(FileName);
                FileName = uri.AbsolutePath.TrimStart('/');
                FileName = FileName.Replace($"{_firebaseStorageService.Bucket}/", "");
            }

            if (FileName.StartsWith("dokumente/dokumente/"))
                FileName = FileName.Replace("dokumente/dokumente/", "dokumente/");

            FileName = Uri.UnescapeDataString(FileName);

            if (string.IsNullOrWhiteSpace(FileName))
                return BadRequest("❌ Nom de fichier manquant ou invalide.");
            // ⚡ Toujours conserver le chemin original AVANT conversion
            if (string.IsNullOrWhiteSpace(OriginalPath))
                OriginalPath = FileName;

            // ✅ Vérifie si ce n’est pas un PDF → conversion
            if (!FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("⚡ Conversion de {FileName} en PDF avant affichage", FileName);

                using var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(FileName, ms);
                var fileBytes = ms.ToArray();

                var pdfStream = FileConversionHelper.ConvertToPdf(FileName, fileBytes);
                pdfStream.Position = 0;

                string directory = Path.GetDirectoryName(FileName) ?? string.Empty;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
                string newFileName = fileNameWithoutExt + ".pdf";
                string newPath = Path.Combine(directory, newFileName).Replace("\\", "/");

                await _firebaseStorageService.UploadStreamAsync(pdfStream, newPath, "application/pdf");

                // ⚡ Maintenant, on ne touche PAS à OriginalPath
                // ⚠️ On met uniquement FileName à jour
                FileName = newPath;
            }


            // ✅ Vérifie sur Firebase
            var exists = await _firebaseStorageService.ObjectExistsAsync(FileName);
            if (exists)
            {
                SasUrl = await _firebaseStorageService.GetDownloadUrlAsync(FileName, 15);
            }
            else
            {
                SasUrl = null;
                _logger.LogWarning("❌ Fichier introuvable dans Firebase : {FileName}", FileName);
            }

            return Page();
        }


        public class SignaturePayload
        {
            public string? FileName { get; set; }       // PDF après conversion
            public string? OriginalPath { get; set; }   // Chemin original (jpg, png, docx...)
            public string? FileType { get; set; }
            public string? ImageBase64 { get; set; }
            public int PageNumber { get; set; } = 1;
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public float CanvasWidth { get; set; } = 900f;
            public float CanvasHeight { get; set; } = 1270f;
            public string? TextHtml { get; set; }
            public float TextX { get; set; }
            public float TextY { get; set; }
            public float TextWidth { get; set; }
            public float TextHeight { get; set; }
            public string? Metadaten { get; set; }
            public string? FirmaName { get; set; }
            public string? AbteilungName { get; set; }
            public string? Kategorie { get; set; }
        }

    }
}
