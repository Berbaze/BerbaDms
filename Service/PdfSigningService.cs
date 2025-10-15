using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;
using DmsProjeckt.Helpers;

namespace DmsProjeckt.Service
{
    public class PdfSigningService
    {
        private readonly FirebaseStorageService _firebaseStorage;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PdfSigningService> _logger;

        public PdfSigningService(
            FirebaseStorageService firebaseStorage,
            ApplicationDbContext db,
            ILogger<PdfSigningService> logger)
        {
            _firebaseStorage = firebaseStorage;
            _db = db;
            _logger = logger;
        }

        // Méthodes PDF X : inchangées
        public byte[] AddImageSignature(
   byte[] originalPdf,
   string base64Image,
   int pageNumber,
   float x,
   float y,
   float? width = null,
   float? height = null,
   float? canvasWidth = null,
   float? canvasHeight = null)
        {
            using var msInput = new MemoryStream(originalPdf);
            using var doc = PdfReader.Open(msInput, PdfDocumentOpenMode.Modify);

            if (pageNumber < 1 || pageNumber > doc.PageCount)
                pageNumber = 1;

            var targetPage = doc.Pages[pageNumber - 1];
            var gfx = XGraphics.FromPdfPage(targetPage);

            var imageBytes = Convert.FromBase64String(base64Image.Split(',')[1]);
            using var imgStream = new MemoryStream(imageBytes);
            using var img = XImage.FromStream(() => imgStream);

            float pageWidth = (float)targetPage.Width;
            float pageHeight = (float)targetPage.Height;

            // 📏 Signature dimensions
            float imgWidth = width ?? 100f;
            float imgHeight = height ?? 50f;

            // 🧠 Si les dimensions canvas sont fournies → convertir
            if (canvasWidth.HasValue && canvasHeight.HasValue && width.HasValue && height.HasValue)
            {
                x *= (pageWidth / canvasWidth.Value);
                y *= (pageHeight / canvasHeight.Value);
                imgWidth *= (pageWidth / canvasWidth.Value);
                imgHeight *= (pageHeight / canvasHeight.Value);
            }


            float adjustedY = pageHeight - y - imgHeight;

            // 🖋️ Signature
            gfx.DrawImage(img, x, adjustedY, imgWidth, imgHeight);

            var output = new MemoryStream();
            doc.Save(output, false);
            output.Position = 0;

            return output.ToArray();
        }
        public async Task<MemoryStream> AddSignatureToPdfAsync(
           Stream originalPdf,
           string imageBase64,
           int page,
           float x,
           float y,
           float? width = null,
           float? height = null,
           float canvasWidth = 900f,
           float canvasHeight = 1270f,
           string firmaName = null,
           bool addSignatureInfo = false)
        {
            _logger.LogInformation("[DEBUG] addSignatureInfo={Info}, firmaName='{Name}'", addSignatureInfo, firmaName);

            using var doc = PdfReader.Open(originalPdf, PdfDocumentOpenMode.Modify);

            if (page < 1 || page > doc.PageCount)
                page = 1;

            var targetPage = doc.Pages[page - 1];
            var gfx = XGraphics.FromPdfPage(targetPage);

            if (string.IsNullOrWhiteSpace(imageBase64) || !imageBase64.Contains(','))
                throw new ArgumentException("❌ Signature Base64 invalide");

            var imageBytes = Convert.FromBase64String(imageBase64.Split(',')[1]);
            using var imgStream = new MemoryStream(imageBytes);
            using var img = XImage.FromStream(() => imgStream);

            float pageWidth = (float)targetPage.Width;
            float pageHeight = (float)targetPage.Height;

            float pdfX = x * (pageWidth / canvasWidth);
            float pdfY = y * (pageHeight / canvasHeight);
            float pdfWidth = (width ?? 100f) * (pageWidth / canvasWidth);
            float pdfHeight = (height ?? 50f) * (pageHeight / canvasHeight);

            float adjustedY = pageHeight - pdfY - pdfHeight;

            gfx.DrawImage(img, pdfX, adjustedY, pdfWidth, pdfHeight);

            if (addSignatureInfo && !string.IsNullOrWhiteSpace(firmaName))
            {
                string dateStr = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                string text = $"Signiert am: {dateStr}\nFirma: {firmaName}";

                var font = new XFont("Helvetica", 10, XFontStyle.Bold);
                var textX = pdfX;
                var textY = adjustedY + pdfHeight + 5; // juste sous la signature

                gfx.DrawString(text, font, XBrushes.Black, new XRect(textX, textY, pageWidth - textX, 40), XStringFormats.TopLeft);


                _logger.LogInformation("🖊 Texte ajouté sous la signature : {Text}", text);
            }

            _logger.LogInformation("📍 Signature ajoutée : page={Page}, x={X}, y={Y}, width={W}, height={H}", page, pdfX, adjustedY, pdfWidth, pdfHeight);

            var output = new MemoryStream();
            doc.Save(output, false);
            output.Position = 0;

            return output;
        }

        public async Task<string> SaveSignedVersionAsync(
            string originalPath,   // chemin du fichier tel qu’en DB (jpg, png…)
            string pdfPath,        // chemin PDF (après conversion) utilisé pour signer
            string base64Image,
            int page,
            float x,
            float y,
            float width,
            float height,
            float canvasWidth,
            float canvasHeight,
            string applicationUserId,
            string firmaName,
            string? metadatenJson)
        {
            _logger.LogInformation("[SIGN] Starte Signatur-Workflow für Original={Original}, PDF={Pdf}", originalPath, pdfPath);

            // 🔎 Retrouver le document original dans la DB
            var parentDoc = await _db.Dokumente
                .Include(d => d.Abteilung)
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(d => d.ObjectPath == NormalizePath(originalPath));

            if (parentDoc == null)
                throw new Exception($"❌ Original-Dokument nicht gefunden! objectPath={originalPath}");

            // 🔎 Charger l’utilisateur
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == applicationUserId);
            if (user == null)
                throw new Exception("❌ Benutzer nicht gefunden");

            // 🔒 Vérification des rôles
            var isAdmin = await (
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                where ur.UserId == user.Id && (r.Name == "Admin" || r.Name == "SuperAdmin")
                select ur
            ).AnyAsync();

            if (!isAdmin && user.AbteilungId != parentDoc.AbteilungId)
                throw new UnauthorizedAccessException("❌ Sie dürfen nur Dokumente Ihrer eigenen Abteilung signieren.");

            // 📂 Nouveau chemin pour la version signée
            var firma = parentDoc.ObjectPath?.Split('/')[1];
            var abtName = parentDoc.Abteilung?.Name ?? "allgemein";
            string kategorie = "UnterlagenSigniert";

            var (newPath, abteilungId) = DocumentPathHelper.BuildFinalPath(
                firma!,
                $"{Path.GetFileNameWithoutExtension(originalPath)}_signed_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                kategorie,
                parentDoc.AbteilungId,
                abtName
            );

            // 📥 Télécharger le fichier PDF (toujours celui converti)
            using var downloadStream = new MemoryStream();
            await _firebaseStorage.DownloadToStreamAsync(pdfPath, downloadStream);
            var fileBytes = downloadStream.ToArray();

            using var pdfStream = new MemoryStream(fileBytes);

            // ✍️ Ajouter signature
            using var signedStream = await AddSignatureToPdfAsync(
                pdfStream, base64Image, page, x, y, width, height,
                canvasWidth, canvasHeight, firmaName, addSignatureInfo: true
            );

            // 📤 Uploader la version signée
            await _firebaseStorage.UploadStreamAsync(signedStream, newPath, "application/pdf");

            // 🆕 Créer l’entrée DB
            var signedDoc = new Dokumente
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                ApplicationUser = user,
                KundeId = parentDoc.KundeId,
                Dateiname = Path.GetFileName(newPath),
                Dateipfad = $"https://storage.googleapis.com/{_firebaseStorage.Bucket}/{newPath}",
                ObjectPath = NormalizePath(newPath),
                HochgeladenAm = DateTime.UtcNow,

                AbteilungId = abteilungId,
                IsVersion = true,
                EstSigne = true,

                Kategorie = !string.IsNullOrWhiteSpace(parentDoc.Kategorie)
                         ? parentDoc.Kategorie
                         : kategorie,

                OriginalId = parentDoc.Id
            };

            // ✅ Appliquer métadonnées + signé par
            string signedBy = $"{user.Vorname} {user.Nachname}";
            DocumentPathHelper.ApplyMetadataToDocument(signedDoc, parentDoc, metadatenJson, signedBy);

            _db.Dokumente.Add(signedDoc);
            await _db.SaveChangesAsync();

            return signedDoc.Dateiname!;
        }



        private async Task<MemoryStream> AddSignatureAndTextToPdfAsync(
    Stream originalPdf,
    string imageBase64,
    string text,
    int page,
    float sigX, float sigY, float sigW, float sigH,
    float txtX, float txtY, float txtW, float txtH,
    float canvasW, float canvasH)
        {
            using var doc = PdfReader.Open(originalPdf, PdfDocumentOpenMode.Modify);
            var pageObj = doc.Pages[Math.Clamp(page - 1, 0, doc.PageCount - 1)];
            var gfx = XGraphics.FromPdfPage(pageObj);

            float pdfW = (float)pageObj.Width;
            float pdfH = (float)pageObj.Height;

            // Signature
            var sigImgBytes = Convert.FromBase64String(imageBase64.Split(',')[1]);
            using var sigStream = new MemoryStream(sigImgBytes);
            using var sigImg = XImage.FromStream(() => sigStream);

            float sx = sigX * (pdfW / canvasW);
            float sy = sigY * (pdfH / canvasH);
            float sw = sigW * (pdfW / canvasW);
            float sh = sigH * (pdfH / canvasH);
            float adjSigY = pdfH - sy - sh;

            gfx.DrawImage(sigImg, sx, adjSigY, sw, sh);

            // Texte
            if (!string.IsNullOrWhiteSpace(text))
            {
                string cleanedText = text
                    .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
                    .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
                    .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase);

                float tx = txtX * (pdfW / canvasW);
                float ty = txtY * (pdfH / canvasH);
                float tw = txtW * (pdfW / canvasW);
                float th = txtH * (pdfH / canvasH);
                float adjTextY = pdfH - ty - th;

                var font = new XFont("Helvetica", 10, XFontStyle.Regular);
                var lineHeight = font.Size + 2;

                string[] lines = cleanedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    var y = adjTextY + i * lineHeight;
                    var layout = new XRect(tx, y, tw, lineHeight);
                    gfx.DrawString(lines[i], font, XBrushes.Black, layout, XStringFormats.TopLeft);
                }
            }

            var output = new MemoryStream();
            doc.Save(output, false);
            output.Position = 0;

            return output;
        }

        private string ExtractAbteilung(string kategorie)
        {
            if (string.IsNullOrWhiteSpace(kategorie))
                return "Allgemein";

            var parts = kategorie.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return parts[2]; // "allgemein", "rechnungen", etc.

            return "Allgemein";
        }
        public async Task<string> SaveSignedVersionWithTextAsync(
       string objectPath,
       string base64Image,
       int page,
       float x,
       float y,
       float width,
       float height,
       float canvasWidth,
       float canvasHeight,
       string applicationUserId,
       string customText,
       float textX,
       float textY,
       float textWidth,
       float textHeight,
       string? metadatenJson)
        {
            _logger.LogInformation("[SIGN+TEXT] Start für {Path}", objectPath);

            var parentDoc = await _db.Dokumente
                .Include(d => d.Abteilung)
                .FirstOrDefaultAsync(d => d.ObjectPath == NormalizePath(objectPath));

            if (parentDoc == null)
                throw new Exception($"❌ Original-Dokument nicht gefunden! objectPath={objectPath}");

            var firma = parentDoc.ObjectPath?.Split('/')[1];
            var abtName = parentDoc.Abteilung?.Name ?? "allgemein";
            string kategorie = "UnterlagenSigniert";

            var (newPath, abteilungId) = DocumentPathHelper.BuildFinalPath(
                firma!,
                $"{Path.GetFileNameWithoutExtension(objectPath)}_signed_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                kategorie,
                parentDoc.AbteilungId,
                abtName
            );

            byte[] fileBytes;
            using (var downloadStream = new MemoryStream())
            {
                await _firebaseStorage.DownloadToStreamAsync(objectPath, downloadStream);
                fileBytes = downloadStream.ToArray();
            }

            if (this.IsImage(objectPath))
            {
                fileBytes = this.ConvertImageToPdf(fileBytes);
                objectPath = Path.ChangeExtension(objectPath, ".pdf");
            }

            using var pdfStream = new MemoryStream(fileBytes);
            var signedStream = await AddSignatureAndTextToPdfAsync(
                pdfStream,
                base64Image,
                customText,
                page,
                x, y, width, height,
                textX, textY, textWidth, textHeight,
                canvasWidth, canvasHeight);

            signedStream.Position = 0;

            await _firebaseStorage.UploadStreamAsync(signedStream, newPath, "application/pdf");

            var signedDoc = new Dokumente
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = applicationUserId,
                KundeId = parentDoc.KundeId,
                Dateiname = Path.GetFileName(newPath),
                Dateipfad = newPath,
                ObjectPath = NormalizePath(newPath),
                HochgeladenAm = DateTime.UtcNow,

                AbteilungId = abteilungId,
                IsVersion = true,
                EstSigne = true,

                // ✅ Fix Kategorie
                Kategorie = !string.IsNullOrWhiteSpace(parentDoc.Kategorie)
                      ? parentDoc.Kategorie
                      : "UnterlagenSigniert",

                OriginalId = parentDoc.Id,
                Titel = parentDoc.Titel,
                Beschreibung = "Signierte Version mit Text"
            };

            DocumentPathHelper.ApplyMetadataToDocument(signedDoc, parentDoc, metadatenJson);

            _db.Dokumente.Add(signedDoc);
            await _db.SaveChangesAsync();

            return signedDoc.Dateiname!;
        }



        public byte[] ConvertImageToPdf(byte[] imageBytes)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var debugPdfPath = Path.Combine(desktopPath, "DEBUG_IMAGE2PDF.pdf");

            try
            {
                _logger.LogInformation("[DEBUG] Entrée dans ConvertImageToPdf");

                using var msImage = new MemoryStream(imageBytes);
                using var image = System.Drawing.Image.FromStream(msImage);

                using var msPdf = new MemoryStream();
                using (var doc = new PdfSharpCore.Pdf.PdfDocument())
                {
                    var page = doc.AddPage();
                    page.Width = image.Width;
                    page.Height = image.Height;

                    using (var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page))
                    {
                        using var msPng = new MemoryStream();
                        image.Save(msPng, System.Drawing.Imaging.ImageFormat.Png);
                        msPng.Position = 0;

                        using var xImg = PdfSharpCore.Drawing.XImage.FromStream(() => msPng);

                        gfx.DrawImage(xImg, 0, 0, image.Width, image.Height);
                    }
                    doc.Save(msPdf, false);
                }

                File.WriteAllBytes(debugPdfPath, msPdf.ToArray());

                _logger.LogInformation("[DEBUG] PDF généré sur le bureau.");

                return msPdf.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEBUG] Erreur dans ConvertImageToPdf : {Message}", ex.Message);
                throw;
            }
        }



        public bool IsImage(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }
        public byte[] NormalizeImage(byte[] imageBytes)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var image = System.Drawing.Image.FromStream(inputStream);
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png); // ou .Jpeg si tu préfères
            return ms.ToArray();
        }

        public async Task<string> SaveSignedImageVersionAsync(
     string objectPath,
     string base64Image,
     float sigX, float sigY, float sigW, float sigH,
     string applicationUserId,
     string customText,
     float textX, float textY, float textW, float textH,
     string? metadatenJson)
        {
            _logger.LogInformation("[IMAGE SIGN] Start für {Path}", objectPath);

            var parentDoc = await _db.Dokumente
                .Include(d => d.Abteilung)
                .FirstOrDefaultAsync(d => d.ObjectPath == NormalizePath(objectPath));

            if (parentDoc == null)
                throw new Exception("❌ Original-Dokument nicht gefunden");

            var firma = parentDoc.ObjectPath?.Split('/')[1];
            var abtName = parentDoc.Abteilung?.Name ?? "allgemein";
            string kategorie = "UnterlagenSigniert";

            var (newPath, abteilungId) = DocumentPathHelper.BuildFinalPath(
                firma!,
                $"{Path.GetFileNameWithoutExtension(objectPath)}_signed_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(objectPath)}",
                kategorie,
                parentDoc.AbteilungId,
                abtName
            );

            byte[] imageBytes;
            using (var downloadStream = new MemoryStream())
            {
                await _firebaseStorage.DownloadToStreamAsync(objectPath, downloadStream);
                imageBytes = downloadStream.ToArray();
            }

            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

            if (!string.IsNullOrEmpty(base64Image))
            {
                var sigBytes = Convert.FromBase64String(base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image);
                using var signatureImg = SixLabors.ImageSharp.Image.Load<Rgba32>(sigBytes);
                signatureImg.Mutate(x => x.Resize((int)sigW, (int)sigH));
                image.Mutate(x => x.DrawImage(signatureImg, new SixLabors.ImageSharp.Point((int)sigX, (int)sigY), 1f));
            }

            if (string.IsNullOrWhiteSpace(customText))
            {
                string dateStr = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                customText = $"Signiert am: {dateStr}\nFirma: {firma}";
            }

            if (!string.IsNullOrEmpty(customText))
            {
                var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 18, SixLabors.Fonts.FontStyle.Bold);
                var options = new SixLabors.Fonts.TextOptions(font)
                {
                    HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment.Left,
                    VerticalAlignment = SixLabors.Fonts.VerticalAlignment.Top,
                    Origin = new SixLabors.ImageSharp.PointF(textX, textY)
                };
                image.Mutate(x => x.DrawText(options, customText, SixLabors.ImageSharp.Color.Black));
            }

            using var ms = new MemoryStream();
            string ext = Path.GetExtension(objectPath).ToLower();
            if (ext == ".jpg" || ext == ".jpeg")
                await image.SaveAsJpegAsync(ms);
            else
                await image.SaveAsPngAsync(ms);

            ms.Position = 0;
            await _firebaseStorage.UploadStreamAsync(ms, newPath, (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" : "image/png");

            var signedDoc = new Dokumente
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = applicationUserId,
                KundeId = parentDoc?.KundeId,
                Dateiname = Path.GetFileName(newPath),
                Dateipfad = newPath,
                ObjectPath = NormalizePath(newPath),
                HochgeladenAm = DateTime.UtcNow,

                AbteilungId = abteilungId,
                IsVersion = true,
                EstSigne = true,

                // ✅ Fix Kategorie
                Kategorie = !string.IsNullOrWhiteSpace(parentDoc?.Kategorie)
                        ? parentDoc.Kategorie
                        : "UnterlagenSigniert",

                OriginalId = parentDoc?.Id ?? Guid.Empty,
                Titel = parentDoc?.Titel ?? "Signierte Bild",
                Beschreibung = "Signierte Version mit Text"
            };
            DocumentPathHelper.ApplyMetadataToDocument(signedDoc, parentDoc, metadatenJson);


            _db.Dokumente.Add(signedDoc);
            await _db.SaveChangesAsync();

 
            _db.Dokumente.Add(signedDoc);
            await _db.SaveChangesAsync();

            return signedDoc.Dateiname!;
        }


        private string NormalizePath(string path)
        {
            return path?.Replace("\\", "/").Trim();
        }


    }
}
