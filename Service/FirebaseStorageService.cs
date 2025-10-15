using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using DmsProjeckt.Data;
using DmsProjeckt.Pages.Dokument;
using DmsProjeckt.Service;
using Firebase.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static DmsProjeckt.Pages.Dokument.IndexModel;
using System.Text.Json;


namespace DmsProjeckt.Service
{
    public class FirebaseStorageService
    {
        private readonly StorageClient _storage;
        private readonly string _bucket;
        private readonly MyFirebaseOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ApplicationDbContext _db;
        private readonly UrlSigner _signer;
        public string Bucket => _bucket;


        public FirebaseStorageService(IConfiguration config, ApplicationDbContext db, MyFirebaseOptions options, IHttpContextAccessor httpContextAccessor)
        {
            var credPath = config["Firebase:ServiceAccountPath"];
            _bucket = config["Firebase:Bucket"];

            if (string.IsNullOrWhiteSpace(_bucket)) throw new Exception("⚠️ Bucket Name fehlt!");
            GoogleCredential credential = GoogleCredential.FromFile(credPath);
            _storage = StorageClient.Create(credential);
            _signer = UrlSigner.FromServiceAccountPath(credPath);
            _db = db;
            _options = options;
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        }
        private bool IsFile(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.Contains('.') && !fileName.EndsWith(".");
        }
        public async Task<List<DmsFolder>> BuildExplorerTreeAsync(string rootPath, List<Dokumente> allDocs)
        {
            Console.WriteLine($"🚀 [START] BuildExplorerTreeAsync | rootPath={rootPath}");

            rootPath = rootPath?.TrimEnd('/') ?? string.Empty;
            var explorerTree = new List<DmsFolder>();
            int totalFiles = 0, linkedMeta = 0;

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                Console.WriteLine("❌ rootPath ist leer oder null!");
                return explorerTree;
            }

            _db.ChangeTracker.Clear();

            // 🧩 Lade NUR relevante Metadaten (verbunden mit den Dokumenten)
            var dokumentIds = allDocs.Select(d => d.Id).ToList();

            var allMetas = await _db.Metadaten
                .AsNoTracking()
                .Where(m => m.DokumentId != null && dokumentIds.Contains(m.DokumentId.Value))
                .ToListAsync();

            // 🔗 Dictionary für Metadaten
            var metaDict = allMetas.ToDictionary(m => m.DokumentId!.Value, m => m);

            // 🔗 Verknüpfe Metadaten mit Dokumenten
            foreach (var doc in allDocs)
            {
                if (metaDict.TryGetValue(doc.Id, out var meta))
                {
                    doc.MetadatenObjekt = meta;
                    linkedMeta++;
                }
            }

            Console.WriteLine($"✅ Metadaten erfolgreich verknüpft: {linkedMeta}/{allDocs.Count}");

            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 🔁 Durchlaufe alle Objekte im Storage
            await foreach (var obj in _storage.ListObjectsAsync(_bucket, $"{rootPath}/"))
            {
                if (string.IsNullOrWhiteSpace(obj.Name))
                    continue;

                // 🚫 Ignorer les dossiers ou fichiers de chunks
                if (obj.Name.Contains("/chunks/", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🧩 Chunk-Verzeichnis ignoré: {obj.Name}");
                    continue;
                }

                var fileName = Path.GetFileName(obj.Name);

                // 🚫 Ignorer les fichiers chunk_*.bin
                if (fileName.StartsWith("chunk_", StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🧩 Chunk-Datei ignoré: {fileName}");
                    continue;
                }

                var normalizedPath = obj.Name.Trim().Replace("\\", "/").ToLowerInvariant();

                // ⚠️ Ignoriere doppelte Pfade
                if (!seenPaths.Add(normalizedPath))
                    continue;
                var parts = obj.Name.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;
                if (!parts[0].Equals("dokumente", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⚠️ Ignoré: {obj.Name} (chemin hors de 'dokumente/')");
                    continue;
                }


                // 🧩 Trouver les segments dynamiquement
                string firma = parts.ElementAtOrDefault(1) ?? "berba";
                string abteilung = parts.ElementAtOrDefault(2) ?? "allgemein";

                // 🔍 Détecter automatiquement la catégorie s’il y a un 4ᵉ segment
                string kategorie = parts.Length > 3 ? parts[3] : "allgemein";

                // 🔧 Normaliser les noms
                firma = firma.Trim().ToLowerInvariant();
                abteilung = abteilung.Trim().ToLowerInvariant();
                kategorie = kategorie.Trim().ToLowerInvariant();

                string abteilungPath = $"dokumente/{firma}/{abteilung}";
                string catPath = $"{abteilungPath}/{kategorie}";


                // 📁 Abteilung anlegen
                var abteilungFolder = explorerTree.FirstOrDefault(f => f.Path.Equals(abteilungPath, StringComparison.OrdinalIgnoreCase));
                if (abteilungFolder == null)
                {
                    abteilungFolder = new DmsFolder
                    {
                        Name = abteilung,
                        Path = abteilungPath,
                        IsAbteilung = true,
                        Icon = "fas fa-building text-info",
                        Files = new List<DmsFile>(),
                        SubFolders = new List<DmsFolder>()
                    };
                    explorerTree.Add(abteilungFolder);
                }

                // 📂 Dossier erkannt (Ordner)
                if (obj.Name.EndsWith("/"))
                {
                    string folderName = parts.Last().TrimEnd('/');
                    if (!abteilungFolder.SubFolders.Any(s => s.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase)))
                    {
                        string icon = folderName.ToLower() switch
                        {
                            "archiv" => "fas fa-archive text-warning",
                            "versionen" => "bi bi-layers text-info",
                            _ => "bi bi-folder-fill text-warning"
                        };

                        abteilungFolder.SubFolders.Add(new DmsFolder
                        {
                            Name = folderName,
                            Path = $"{abteilungFolder.Path}/{folderName}",
                            IsAbteilung = false,
                            Icon = icon,
                            Files = new List<DmsFile>(),
                            SubFolders = new List<DmsFolder>()
                        });

                        Console.WriteLine($"🗂️ Ordner hinzugefügt: {folderName}");
                    }
                    continue;
                }

                // 📄 Datei erkannt (fichier réel)
                totalFiles++;

                var dbDoc = allDocs.FirstOrDefault(d =>
                    !string.IsNullOrWhiteSpace(d.ObjectPath) &&
                    d.ObjectPath.Trim().Replace("\\", "/").Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

                // 🧩 Si document chunké → reconstruction automatique
                if (dbDoc != null && dbDoc.IsChunked)
                {
                    Console.WriteLine($"⚙️ Reconstruction automatique détectée pour {dbDoc.Dateiname}");
                    await FinalizeChunkedUploadAsync(dbDoc.Id);

                    // 🔁 Recharger le document après reconstruction
                    dbDoc = await _db.Dokumente.AsNoTracking().FirstOrDefaultAsync(d => d.Id == dbDoc.Id);
                }

                var meta = dbDoc != null && metaDict.TryGetValue(dbDoc.Id, out var m) ? m : null;

                // 🧩 Kategorie bestimmen (Meta > DB > Pfad)
                kategorie = meta?.Kategorie?.Trim()
                    ?? dbDoc?.Kategorie?.Trim()
                    ?? (parts.Length > 3 ? parts[3].Trim() : "Allgemein");

                // ✨ Normalisation
                kategorie = string.IsNullOrWhiteSpace(kategorie) ? "Allgemein" :
                    char.ToUpper(kategorie[0]) + (kategorie.Length > 1 ? kategorie.Substring(1).ToLower() : "");

                // 🔁 Correction für spezielle Ordnernamen
                if (fileName.ToLower().Contains("archiviert") || kategorie.Equals("archiv", StringComparison.OrdinalIgnoreCase))
                    kategorie = "Archiv";

                if (fileName.ToLower().Contains("version") || kategorie.Equals("versionen", StringComparison.OrdinalIgnoreCase))
                    kategorie = "Versionen";

                Console.WriteLine($"🎯 Kategorie entschieden: {kategorie} (DB={dbDoc?.Kategorie}, Meta={meta?.Kategorie}, Pfad={obj.Name})");

                // 🔁 Fallback : si le fichier n’a pas de catégorie mais un dossier parent identifiable
                if (string.IsNullOrWhiteSpace(kategorie) && parts.Length > 3)
                    kategorie = parts[3];

                // ✅ Recherche après fallback
                var catFolder = abteilungFolder.SubFolders
                    .FirstOrDefault(s => s.Name.Equals(kategorie, StringComparison.OrdinalIgnoreCase));


                if (catFolder == null)
                {
                    string icon = kategorie.ToLower() switch
                    {
                        "archiv" => "fas fa-archive text-warning",
                        "versionen" => "bi bi-layers text-info",
                        _ => "bi bi-folder-fill text-warning"
                    };

                    catFolder = new DmsFolder
                    {
                        Name = kategorie,
                        Path = $"{abteilungFolder.Path}/{kategorie}",
                        IsAbteilung = false,
                        Icon = icon,
                        Files = new List<DmsFile>(),
                        SubFolders = new List<DmsFolder>()
                    };
                    abteilungFolder.SubFolders.Add(catFolder);
                }

                // 📄 Datei hinzufügen (nur wenn nicht déjà dans le dossier)
                if (!catFolder.Files.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    var file = new DmsFile
                    {
                        Id = dbDoc?.Id.ToString() ?? Guid.NewGuid().ToString(),
                        Name = fileName,
                        Path = obj.Name,
                        ObjectPath = obj.Name,
                        SasUrl = GenerateSignedUrl(obj.Name, 15),
                        Kategorie = kategorie,
                        AbteilungName = abteilung,
                        Beschreibung = meta?.Beschreibung ?? dbDoc?.Beschreibung ?? "",
                        Titel = meta?.Titel ?? dbDoc?.Titel ?? Path.GetFileNameWithoutExtension(fileName),
                        HochgeladenAm = dbDoc?.HochgeladenAm ?? DateTime.UtcNow,
                        Status = dbDoc?.dtStatus.ToString() ?? "Unbekannt",
                        MetadatenObjekt = meta,
                        MetadataJson = JsonSerializer.Serialize(meta, new JsonSerializerOptions
                        {
                            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                            WriteIndented = false
                        })
                    };

                    catFolder.Files.Add(file);
                    Console.WriteLine($"📄 Datei hinzugefügt: {file.Name} | Kat={kategorie} | MetaId={meta?.Id}");
                }
            }
            // 🧩 Ajouter aussi les documents chunkés (chunked://...) même sans fichiers réels
            foreach (var dbDoc in allDocs.Where(d => d.IsChunked && !string.IsNullOrWhiteSpace(d.ObjectPath)))
            {
                try
                {
                    Console.WriteLine($"🧩 Chunked document détecté en DB: {dbDoc.Dateiname} ({dbDoc.ObjectPath})");

                    // ✅ Gestion du format chunked://{guid}
                    string chunkedId = null;
                    if (dbDoc.ObjectPath.StartsWith("chunked://", StringComparison.OrdinalIgnoreCase))
                    {
                        chunkedId = dbDoc.ObjectPath.Replace("chunked://", "").Trim();
                    }

                    // 🔍 Récupère le dossier Firebase attendu
                    var abt = await _db.Abteilungen.FindAsync(dbDoc.AbteilungId);
                    var abteilung = abt?.Name?.ToLowerInvariant() ?? "allgemein";
                    var firma = "berba"; // ⚠️ adapte selon ton environnement
                    var kategorie = dbDoc.Kategorie?.ToLowerInvariant() ?? "allgemein";

                    string abteilungPath = $"dokumente/{firma}/{abteilung}";
                    string catPath = $"{abteilungPath}/{kategorie}";

                    // 📁 Crée l'abteilung si absente
                    var abteilungFolder = explorerTree.FirstOrDefault(f => f.Path.Equals(abteilungPath, StringComparison.OrdinalIgnoreCase));
                    if (abteilungFolder == null)
                    {
                        abteilungFolder = new DmsFolder
                        {
                            Name = abteilung,
                            Path = abteilungPath,
                            IsAbteilung = true,
                            Icon = "fas fa-building text-info",
                            Files = new List<DmsFile>(),
                            SubFolders = new List<DmsFolder>()
                        };
                        explorerTree.Add(abteilungFolder);
                    }

                    // 📂 Crée la catégorie si absente
                    var catFolder = abteilungFolder.SubFolders.FirstOrDefault(f => f.Path.Equals(catPath, StringComparison.OrdinalIgnoreCase));
                    if (catFolder == null)
                    {
                        catFolder = new DmsFolder
                        {
                            Name = kategorie,
                            Path = catPath,
                            IsAbteilung = false,
                            Icon = "bi bi-folder-fill text-warning",
                            Files = new List<DmsFile>(),
                            SubFolders = new List<DmsFolder>()
                        };
                        abteilungFolder.SubFolders.Add(catFolder);
                    }

                    // 📄 Crée le fichier virtuel
                    var file = new DmsFile
                    {
                        Id = dbDoc.Id.ToString(),
                        Name = dbDoc.Dateiname ?? $"chunked_{chunkedId}.pdf",
                        Path = $"dokumente/{firma}/{abteilung}/{kategorie}/chunks/{chunkedId}/",
                        ObjectPath = dbDoc.ObjectPath,
                        Kategorie = kategorie,
                        AbteilungName = abteilung,
                        Beschreibung = dbDoc.Beschreibung ?? "Wird rekonstruiert",
                        Titel = dbDoc.Titel ?? dbDoc.Dateiname ?? "Chunked document",
                        HochgeladenAm = dbDoc.HochgeladenAm,
                        Status = "Wird in Teile hochgeladen",
                        IsVersion = false,
                        EstSigne = false,
                        IsIndexed = false,
                        SasUrl = null,
                        IsChunked = true 
                    };

                    if (!catFolder.Files.Any(f => f.Id == file.Id))
                    {
                        catFolder.Files.Add(file);
                        Console.WriteLine($"📦 Document chunké ajouté virtuellement : {file.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur ajout chunked file: {ex.Message}");
                }
            }


            Console.WriteLine($"✅ BuildExplorerTreeAsync abgeschlossen | Dateien={totalFiles}, Metadaten verknüpft={linkedMeta}");
            return explorerTree.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task FinalizeChunkedUploadAsync(Guid dokumentId)
        {
            var dokument = await _db.Dokumente.FirstOrDefaultAsync(d => d.Id == dokumentId);
            if (dokument == null)
                return;

            if (!dokument.IsChunked)
                return; // Rien à faire

            string firma = dokument.ApplicationUser?.FirmenName ?? "unknown";
            string abteilung = dokument.Abteilung?.Name ?? "allgemein";
            string category = dokument.Kategorie ?? "Allgemein";

            string targetPath = $"dokumente/{firma}/{abteilung}/{category}/{dokument.Dateiname}";

            Console.WriteLine($"🔄 Reconstruction automatique de {dokument.Dateiname}...");

            // 🔹 Récupérer les chunks et reconstruire le fichier
            var chunks = await _db.DokumentChunks
                .Where(c => c.DokumentId == dokumentId)
                .OrderBy(c => c.Index)
                .ToListAsync();

            using var combinedStream = new MemoryStream();
            foreach (var chunk in chunks)
            {
                using var chunkStream = await DownloadChunkAsync(chunk.FirebasePath);
                await chunkStream.CopyToAsync(combinedStream);
            }

            combinedStream.Position = 0;

            await UploadStreamAsync(combinedStream, targetPath, "application/pdf");

            dokument.ObjectPath = targetPath;
            dokument.Dateipfad = $"https://storage.googleapis.com/{_bucket}/{targetPath}";
            dokument.IsChunked = false;
            dokument.dtStatus = DokumentStatus.Fertig;

            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Reconstruction terminée et fichier visible dans Explorer !");
        }

        // 🔹 Upload d’un chunk individuel vers Firebase
        public async Task UploadChunkAsync(Stream chunkStream, string firebasePath)
        {
            try
            {
                // ⚙️ Passer le contentType requis ("application/octet-stream" pour des données binaires)
                await UploadStreamAsync(chunkStream, firebasePath, "application/octet-stream");
                Console.WriteLine($"✅ Chunk uploadé sur Firebase: {firebasePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur upload chunk {firebasePath}: {ex.Message}");
                throw;
            }
        }

        // 🔹 Téléchargement d’un chunk depuis Firebase
        public async Task<Stream> DownloadChunkAsync(string firebasePath)
        {
            try
            {
                // ⚙️ Utiliser ta méthode existante GetFileStreamAsync()
                var stream = await GetFileStreamAsync(firebasePath);

                if (stream == null)
                    throw new Exception($"❌ Chunk introuvable sur Firebase: {firebasePath}");

                Console.WriteLine($"📥 Chunk téléchargé depuis Firebase: {firebasePath}");
                return stream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur download chunk {firebasePath}: {ex.Message}");
                throw;
            }
        }



        private string NormalizeKategorie(string kat)
        {
            if (string.IsNullOrWhiteSpace(kat)) return "Allgemein";

            // Vereinheitliche bekannte Ordnernamen
            switch (kat.Trim().ToLower())
            {
                case "archiv":
                    return "Archiv";
                case "versionen":
                    return "Versionen";
                case "rechnungen":
                    return "Rechnungen";
                case "signatures":
                    return "Signatures";
                default:
                    return char.ToUpper(kat[0]) + kat.Substring(1);
            }
        }


        private void RemoveEmptyFolders(DmsFolder folder)
        {
            if (folder.SubFolders != null)
            {
                // On nettoie d’abord récursivement les enfants
                foreach (var sub in folder.SubFolders.ToList())
                {
                    RemoveEmptyFolders(sub);

                    // 🚫 Supprimer si le sous-dossier est vide
                    if (!sub.Files.Any() && !sub.SubFolders.Any())
                    {
                        folder.SubFolders.Remove(sub);
                    }
                }
            }
        }





        public async Task<List<DmsFolder>> GetFolderTreeAsync(string prefix)
        {
            prefix = prefix?.TrimEnd('/') + "/";
            var allFolders = new Dictionary<string, DmsFolder>(StringComparer.OrdinalIgnoreCase);

            var dbDocs = await _db.Dokumente
                .Include(d => d.Abteilung)
                .Include(d => d.MetadatenObjekt)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] {dbDocs.Count} Dateien in DB.");

            int countObj = 0;

            await foreach (var obj in _storage.ListObjectsAsync(_bucket, prefix))
            {
                var path = obj.Name;
                if (string.IsNullOrWhiteSpace(path) || path.EndsWith("/"))
                    continue;

                // 🚫 Ignorer les dossiers ou fichiers de chunks
                if (path.Contains("/chunks/", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🧩 Ignoré (chunks): {path}");
                    continue;
                }

                var fileName = Path.GetFileName(path);
                if (fileName.StartsWith("chunk_", StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🧩 Ignoré (chunk-file): {fileName}");
                    continue;
                }

                countObj++;
                Console.WriteLine($"[FIREBASE] {path}");

                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || !parts[0].Equals("dokumente", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 🧩 Détection dynamique
                string firma = parts.ElementAtOrDefault(1) ?? "berba";
                string abteilung = parts.ElementAtOrDefault(2) ?? "allgemein";
                string kategorie = parts.ElementAtOrDefault(3) ?? "allgemein";

                firma = firma.Trim().ToLowerInvariant();
                abteilung = abteilung.Trim().ToLowerInvariant();
                kategorie = kategorie.Trim().ToLowerInvariant();

                // 🔎 Correspondance DB
                var dbDoc = dbDocs.FirstOrDefault(d =>
                    !string.IsNullOrWhiteSpace(d.ObjectPath) &&
                    d.ObjectPath.Trim().Replace("\\", "/").Equals(path.Trim(), StringComparison.OrdinalIgnoreCase));

                // 🧩 Reconstruction automatique si chunké
                if (dbDoc != null && dbDoc.IsChunked)
                {
                    Console.WriteLine($"⚙️ Reconstruction automatique détectée pour {dbDoc.Dateiname}");
                    await FinalizeChunkedUploadAsync(dbDoc.Id);
                    dbDoc = await _db.Dokumente.Include(d => d.Abteilung).Include(d => d.MetadatenObjekt).AsNoTracking().FirstOrDefaultAsync(d => d.Id == dbDoc.Id);
                }

                // 🔗 Détermination dynamique Abteilung / Kategorie
                var meta = dbDoc?.MetadatenObjekt;
                string abtName = dbDoc?.Abteilung?.Name ?? abteilung;
                string katName = meta?.Kategorie ?? kategorie;

                // 📄 Création du fichier
                var file = new DmsFile
                {
                    Id = dbDoc?.Id.ToString() ?? Guid.NewGuid().ToString(),
                    Name = fileName,
                    Path = path,
                    SasUrl = GenerateSignedUrl(path, 15),
                    ObjectPath = path,
                    Kategorie = katName,
                    AbteilungName = abtName,
                    Beschreibung = meta?.Beschreibung ?? dbDoc?.Beschreibung,
                    Titel = meta?.Titel ?? dbDoc?.Titel ?? Path.GetFileNameWithoutExtension(fileName),
                    HochgeladenAm = dbDoc?.HochgeladenAm,
                    Status = dbDoc?.dtStatus.ToString() ?? "Unbekannt",
                    IsIndexed = dbDoc?.IsIndexed,
                    EstSigne = dbDoc?.EstSigne ?? false
                };

                // 📁 Arborescence dynamique
                string abteilungPath = $"dokumente/{firma}/{abtName}";
                string catPath = $"{abteilungPath}/{katName}";

                // 🔧 Crée l’abteilung si absente
                if (!allFolders.TryGetValue(abteilungPath, out var abteilungFolder))
                {
                    abteilungFolder = new DmsFolder
                    {
                        Name = abtName,
                        Path = abteilungPath,
                        IsAbteilung = true,
                        Icon = "fas fa-building text-info",
                        Files = new List<DmsFile>(),
                        SubFolders = new List<DmsFolder>()
                    };
                    allFolders[abteilungPath] = abteilungFolder;
                }

                // 🔧 Crée la catégorie si absente
                if (!allFolders.TryGetValue(catPath, out var catFolder))
                {
                    catFolder = new DmsFolder
                    {
                        Name = katName,
                        Path = catPath,
                        IsAbteilung = false,
                        Icon = "bi bi-folder-fill text-warning",
                        Files = new List<DmsFile>(),
                        SubFolders = new List<DmsFolder>()
                    };
                    allFolders[catPath] = catFolder;
                    abteilungFolder.SubFolders.Add(catFolder);
                }

                // 📄 Ajoute le fichier
                if (!catFolder.Files.Any(f => f.Name.Equals(file.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    catFolder.Files.Add(file);
                    Console.WriteLine($"📄 {abtName}/{katName} → {file.Name}");
                }
            }

            Console.WriteLine($"[DEBUG] {countObj} objets Firebase lus.");
            Console.WriteLine($"[DEBUG] Dossiers créés: {allFolders.Count}");

            return allFolders.Values
                .Where(f => f.Path.StartsWith("dokumente/") && f.Path.Count(c => c == '/') == 2)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }



        // Déplacer un fichier (copier puis supprimer)
        public async Task<bool> MoveFileAsync(string sourceObjectPath, string destinationObjectPath)
        {
            try
            {
                await _storage.CopyObjectAsync(_bucket, sourceObjectPath, _bucket, destinationObjectPath);
                await _storage.DeleteObjectAsync(_bucket, sourceObjectPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public class MoveRequest
        {
            public string? Source { get; set; }
            public string? Target { get; set; }
        }
        public async Task DownloadToStreamAsync(string path, Stream destination)
        {
            await _storage.DownloadObjectAsync(_bucket, path, destination);
        }

        public async Task UploadStreamAsync(Stream stream, string objectName, string contentType)
        {
            try
            {
                if (stream == null)
                    throw new ArgumentNullException(nameof(stream));

                if (stream.CanSeek)
                    stream.Position = 0;

                // 🔹 Firebase/Google schließt den Stream intern → also lieber eigene Kopie erzeugen
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var buffer = ms.ToArray();

                using var uploadStream = new MemoryStream(buffer);

                await _storage.UploadObjectAsync(
                    bucket: _bucket,
                    objectName: objectName,
                    contentType: contentType,
                    source: uploadStream
                );

                Console.WriteLine($"✅ [Firebase Storage] Upload abgeschlossen → {objectName}");
            }
            catch (Google.GoogleApiException gex)
            {
                Console.WriteLine($"❌ [Firebase Storage API ERROR] {objectName}: {gex.Message}");
                throw;
            }
            catch (IOException ioex)
            {
                Console.WriteLine($"❌ [Firebase Storage IO ERROR] {objectName}: {ioex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Firebase Storage UNKNOWN ERROR] {objectName}: {ex.Message}");
                throw;
            }
        }



        public async Task CreateFolderAsync(string folderPath)
        {
            // Nettoyage du chemin + ajout d’un fichier vide "__init.txt"
            var fullPath = folderPath.Trim().TrimEnd('/') + "/__init.txt";

            using var emptyStream = new MemoryStream(); // fichier vide
            await UploadAsync(emptyStream, fullPath);
        }

        public async Task UploadAsync(Stream stream, string blobPath)
        {
            var task = new FirebaseStorage(
                _options.Bucket,
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(_options.AuthToken),
                    ThrowOnCancel = true
                })
                .Child(blobPath)
                .PutAsync(stream);

            await task;
        }

        public async Task CopyAsync(string sourcePath, string targetPath)
        {
            await _storage.CopyObjectAsync(_bucket, sourcePath, _bucket, targetPath);
        }
        public async Task<bool> DeleteAsync(string path)
        {
            try
            {
                await _storage.DeleteObjectAsync(_bucket, path);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<string> GetDownloadUrlAsync(string path, int minutes = 15)
        {
            // Assure-toi que `path` ne commence pas par une URL complète
            if (path.StartsWith("https://storage.googleapis.com/"))
            {
                // Enlève le préfixe pour ne garder que le chemin dans le bucket
                path = path.Replace($"https://storage.googleapis.com/{_bucket}/", "");
            }

            return _signer.Sign(
                _bucket,
                path,
                TimeSpan.FromMinutes(minutes),
                HttpMethod.Get
            );
        }




        // Vérifier l'existence d'un objet
        public async Task<bool> ObjectExistsAsync(string objectPath)
        {
            try
            {
                var obj = await _storage.GetObjectAsync(_bucket, objectPath);
                return obj != null;
            }
            catch (Google.GoogleApiException e)
            {
                if (e.HttpStatusCode == HttpStatusCode.NotFound)
                    return false;
                throw;
            }
        }
        public async Task CopyFileOnlyAsync(string sourcePath, string targetPath)
        {
            // ✅ Nettoyer les chemins
            string cleanSource = sourcePath
                .Replace($"https://storage.googleapis.com/{_bucket}/", "")
                .Trim();

            string cleanTarget = targetPath
                .Replace($"https://storage.googleapis.com/{_bucket}/", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(cleanSource))
                throw new Exception($"❌ Ungültiger Quellpfad: {sourcePath}");

            if (string.IsNullOrWhiteSpace(cleanTarget))
                throw new Exception($"❌ Ungültiger Zielpfad: {targetPath}");

            // ⚡ Sicherstellen, dass es sich nicht um denselben Pfad handelt
            if (string.Equals(cleanSource, cleanTarget, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"⚠️ CopyFileOnlyAsync ignoriert → gleicher Pfad: {cleanTarget}");
                return;
            }

            // 📂 Datei in Firebase kopieren (ohne Original zu löschen)
            await _storage.CopyObjectAsync(_bucket, cleanSource, _bucket, cleanTarget);
            Console.WriteLine($"📄 Datei kopiert (Original bleibt erhalten): {cleanSource} → {cleanTarget}");
        }


        public async Task<Dictionary<string, object>> GetPropertiesAsync(string path)
        {
            try
            {
                var obj = await _storage.GetObjectAsync(_bucket, path);
                return new Dictionary<string, object>
                {
                    ["Nom"] = Path.GetFileName(obj.Name),
                    ["Taille"] = obj.Size,
                    ["ContentType"] = obj.ContentType,
                    ["Créé"] = obj.TimeCreated,
                    ["Modifié"] = obj.Updated,
                    ["Lien"] = GenerateSignedUrl(obj.Name),
                    ["accessTier"] = obj.StorageClass // facultatif mais utile pour éviter erreur JS
                };
            }
            catch
            {
                return null;
            }
        }


        public async Task<byte[]> LadeDateiAusFirebaseAsync(string bucketName, string objectName)
        {
            StorageClient storageClient = StorageClient.Create(); // Default Credentials, falls Service Account im ENV

            using (var ms = new MemoryStream())
            {
                await storageClient.DownloadObjectAsync(bucketName, objectName, ms);
                return ms.ToArray();
            }
        }




        // Générer un lien signé de téléchargement
        public string? GenerateSignedUrl(string objectName, int minutes = 10)
        {
            try
            {
                return _signer.Sign(
                    _bucket,
                    objectName,
                    TimeSpan.FromMinutes(minutes),
                    HttpMethod.Get
                );
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 🔹 Upload d’un fichier vers Firebase Storage.
        /// Fonctionne à la fois avec Stream et IFormFile.
        /// </summary>
        // ----------------------------------------------------------------------
        // 🔹 Version 1 : Upload depuis un Stream (déjà présente chez toi)
        // ----------------------------------------------------------------------
        public async Task<string> UploadForUserAsync(
            Stream fileStream,
            string fileName,
            string firma,
            string abteilungName,
            string category,
            bool returnPublicUrl = false)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("Der Datei-Stream ist leer oder null.", nameof(fileStream));

            string safeFirma = NormalizeSegment(firma);
            string safeAbteilung = string.IsNullOrWhiteSpace(abteilungName)
                ? "allgemein"
                : NormalizeSegment(abteilungName);
            string safeCategory = string.IsNullOrWhiteSpace(category)
                ? "unbekannt"
                : NormalizeSegment(category);

            string objectName = $"dokumente/{safeFirma}/{safeAbteilung}/{safeCategory}/{fileName}";
            Console.WriteLine($"📤 [Firebase] Upload STREAM → {objectName}");

            try
            {
                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                await _storage.UploadObjectAsync(
                    bucket: _bucket,
                    objectName: objectName,
                    contentType: DetermineContentType(fileName),
                    source: fileStream
                );

                Console.WriteLine($"✅ [Firebase] Upload STREAM erfolgreich → {objectName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Firebase] Upload-Fehler: {ex.Message}");
                throw;
            }

            return returnPublicUrl
                ? $"https://storage.googleapis.com/{_bucket}/{objectName}"
                : objectName;
        }

        // ----------------------------------------------------------------------
        // 🔹 Version 2 : Upload depuis un IFormFile (celle qui te manque !)
        // ----------------------------------------------------------------------
        public async Task<string> UploadForUserAsync(
            IFormFile file,
            string firma,
            string abteilungName,
            string category,
            bool returnPublicUrl = false)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Datei ist leer oder null.", nameof(file));

            using var stream = file.OpenReadStream();
            return await UploadForUserAsync(
                stream,
                file.FileName,
                firma,
                abteilungName,
                category,
                returnPublicUrl
            );
        }



        public async Task<string> MoveFilesAsync(string oldPath, string newPath)
        {
            try
            {
                // 🧩 1️⃣ CAS SPÉCIAL — Fichier "chunked://"
                if (oldPath.StartsWith("chunked://", StringComparison.OrdinalIgnoreCase))
                {
                    string dokumentIdStr = oldPath.Replace("chunked://", "").Trim();

                    if (!Guid.TryParse(dokumentIdStr, out Guid dokumentId))
                        throw new Exception($"❌ Ungültige chunked:// ID: {oldPath}");

                    Console.WriteLine($"🧩 Chunked Dokument erkannt → {dokumentId}");

                    // 🔍 Retrouver le document et la firme
                    var dokument = await _db.Dokumente
                        .Include(d => d.ApplicationUser)
                        .FirstOrDefaultAsync(d => d.Id == dokumentId);

                    if (dokument == null)
                        throw new Exception($"❌ Dokument {dokumentId} nicht gefunden in DB.");

                    string firma = dokument.ApplicationUser?.FirmenName ?? "unknown";
                    string chunkFolder = $"chunks/{firma}/{dokumentId}/";

                    // 🔍 Récupérer les chunks depuis la base
                    var chunks = await _db.DokumentChunks
                        .Where(c => c.DokumentId == dokumentId)
                        .OrderBy(c => c.Index)
                        .ToListAsync();

                    if (chunks.Count == 0)
                        throw new Exception($"❌ Keine Chunks gefunden für {dokumentId} (Pfad: {chunkFolder})");

                    Console.WriteLine($"📦 {chunks.Count} Chunks gefunden → Rekonstruiere Datei...");

                    // 📦 Reconstituer le fichier à partir des chunks
                    using var combinedStream = new MemoryStream();
                    foreach (var chunk in chunks)
                    {
                        using var chunkStream = await DownloadStreamAsync(chunk.FirebasePath);
                        await chunkStream.CopyToAsync(combinedStream);
                    }

                    combinedStream.Position = 0;

                    // ☁️ Uploader le fichier complet vers le nouveau chemin
                    Console.WriteLine($"☁️ Lade rekonstruiertes PDF nach: {newPath}");
                    await UploadStreamAsync(combinedStream, newPath, "application/pdf");

                    // ✅ Mettre à jour les infos du document
                    dokument.ObjectPath = newPath;
                    dokument.Dateipfad = $"https://storage.googleapis.com/{_bucket}/{newPath}";
                    dokument.IsChunked = false;
                    dokument.Beschreibung = "Datei aus Chunks rekonstruiert und verschoben.";
                    dokument.dtStatus = DokumentStatus.Fertig;
                    dokument.IsUpdated = true;

                    await _db.SaveChangesAsync();

                    Console.WriteLine($"✅ Chunked Datei erfolgreich rekonstruiert und verschoben → {newPath}");
                    return newPath;
                }

                // 🟢 2️⃣ CAS NORMAL — Fichier déjà existant dans Firebase
                string cleanOldPath = oldPath
                    .Replace($"https://storage.googleapis.com/{_bucket}/", "")
                    .Trim();

                if (string.IsNullOrWhiteSpace(cleanOldPath))
                    throw new Exception($"❌ Ungültiger alter Pfad: {oldPath}");

                if (string.Equals(cleanOldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⚠️ MoveFilesAsync ignoriert → gleicher Pfad: {newPath}");
                    return newPath;
                }

                // 📁 Copier le fichier vers le nouveau dossier
                await _storage.CopyObjectAsync(_bucket, cleanOldPath, _bucket, newPath);

                // 🧹 Supprimer l’ancien
                await _storage.DeleteObjectAsync(_bucket, cleanOldPath);

                // 🔍 Mettre à jour le document dans la base
                var doc = await _db.Dokumente
                    .Include(d => d.MetadatenObjekt)
                    .FirstOrDefaultAsync(d => d.ObjectPath == cleanOldPath);

                if (doc != null)
                {
                    var parts = newPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    string newKategorie = parts.Length > 3 ? parts[3] : doc.Kategorie;

                    doc.ObjectPath = newPath;
                    doc.Dateipfad = $"https://storage.googleapis.com/{_bucket}/{newPath}";
                    doc.Kategorie = newKategorie;
                    doc.dtStatus = DokumentStatus.Fertig;
                    doc.IsUpdated = true;

                    if (doc.MetadatenObjekt != null)
                        doc.MetadatenObjekt.Kategorie = newKategorie;

                    await _db.SaveChangesAsync();
                    Console.WriteLine($"✅ Dokument aktualisiert: {doc.Dateiname} | Neue Kategorie: {newKategorie}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Kein Dokument in der DB für {cleanOldPath} gefunden.");
                }

                Console.WriteLine($"📦 MoveFilesAsync erfolgreich: {cleanOldPath} → {newPath}");
                return newPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler in MoveFilesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<string> CopyFilesAsync(Guid dokumentId, string sourcePath, string targetPath, int? abteilungId, string kategorie)
        {
            // 🔹 Nettoyer le chemin source (si URL complète)
            string cleanSource = sourcePath
                .Replace($"https://storage.googleapis.com/{_bucket}/", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(cleanSource))
                throw new Exception($"❌ Ungültiger Pfad: {sourcePath}");

            // 📂 Copier le fichier dans Firebase
            await _storage.CopyObjectAsync(_bucket, cleanSource, _bucket, targetPath);

            // 🔍 Charger le document original avec métadonnées
            var originalDoc = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d => d.Id == dokumentId);

            if (originalDoc == null)
                throw new Exception($"❌ Dokument mit ID {dokumentId} nicht gefunden.");

            // ===================== 🧩 DÉTERMINER LA CATÉGORIE =====================
            // Prioriser la catégorie passée en paramètre ou celle des métadonnées
            string finalKategorie = !string.IsNullOrWhiteSpace(kategorie)
                ? kategorie.Trim()
                : originalDoc.MetadatenObjekt?.Kategorie ?? originalDoc.Kategorie ?? "allgemein";

            // ===================== 🧩 NOUVELLES MÉTADONNÉES =====================
            var newMeta = new Metadaten
            {
                Titel = originalDoc.MetadatenObjekt?.Titel,
                Kategorie = finalKategorie,
                Beschreibung = $"Kopie von '{originalDoc.MetadatenObjekt?.Titel ?? originalDoc.Dateiname}'",
                Rechnungsnummer = originalDoc.MetadatenObjekt?.Rechnungsnummer,
                Kundennummer = originalDoc.MetadatenObjekt?.Kundennummer,
                Rechnungsbetrag = originalDoc.MetadatenObjekt?.Rechnungsbetrag,
                Nettobetrag = originalDoc.MetadatenObjekt?.Nettobetrag,
                Gesamtpreis = originalDoc.MetadatenObjekt?.Gesamtpreis,
                Steuerbetrag = originalDoc.MetadatenObjekt?.Steuerbetrag,
                Rechnungsdatum = originalDoc.MetadatenObjekt?.Rechnungsdatum,
                Lieferdatum = originalDoc.MetadatenObjekt?.Lieferdatum,
                Faelligkeitsdatum = originalDoc.MetadatenObjekt?.Faelligkeitsdatum,
                Zahlungsbedingungen = originalDoc.MetadatenObjekt?.Zahlungsbedingungen,
                Lieferart = originalDoc.MetadatenObjekt?.Lieferart,
                ArtikelAnzahl = originalDoc.MetadatenObjekt?.ArtikelAnzahl,
                Email = originalDoc.MetadatenObjekt?.Email,
                Telefon = originalDoc.MetadatenObjekt?.Telefon,
                Telefax = originalDoc.MetadatenObjekt?.Telefax,
                IBAN = originalDoc.MetadatenObjekt?.IBAN,
                BIC = originalDoc.MetadatenObjekt?.BIC,
                Bankverbindung = originalDoc.MetadatenObjekt?.Bankverbindung,
                SteuerNr = originalDoc.MetadatenObjekt?.SteuerNr,
                UIDNummer = originalDoc.MetadatenObjekt?.UIDNummer,
                Adresse = originalDoc.MetadatenObjekt?.Adresse,
                AbsenderAdresse = originalDoc.MetadatenObjekt?.AbsenderAdresse,
                AnsprechPartner = originalDoc.MetadatenObjekt?.AnsprechPartner,
                Zeitraum = originalDoc.MetadatenObjekt?.Zeitraum,
                PdfAutor = originalDoc.MetadatenObjekt?.PdfAutor,
                PdfBetreff = originalDoc.MetadatenObjekt?.PdfBetreff,
                PdfSchluesselwoerter = originalDoc.MetadatenObjekt?.PdfSchluesselwoerter,
                Website = originalDoc.MetadatenObjekt?.Website,
                OCRText = originalDoc.MetadatenObjekt?.OCRText
            };

            await _db.Metadaten.AddAsync(newMeta);
            await _db.SaveChangesAsync();

            // ===================== 🧩 NOUVEAU DOCUMENT =====================
            // 🔹 Chemin relatif (pour Explorer)
            string relativePath = targetPath.StartsWith("dokumente/")
                ? targetPath
                : $"dokumente/{targetPath.TrimStart('/')}";

            var newDoc = new Dokumente
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = originalDoc.ApplicationUserId,
                KundeId = originalDoc.KundeId,
                Titel = originalDoc.Titel,
                Dateiname = Path.GetFileName(targetPath),
                Dateipfad = $"https://storage.googleapis.com/{_bucket}/{targetPath}",
                ObjectPath = targetPath,
                Kategorie = finalKategorie, // ✅ Cohérence DB + Firebase
                Beschreibung = $"Kopie erstellt am {DateTime.UtcNow:dd.MM.yyyy HH:mm}",
                HochgeladenAm = DateTime.UtcNow,
                dtStatus = DokumentStatus.Fertig,
                IsIndexed = false,
                AbteilungId = abteilungId ?? originalDoc.AbteilungId,
                MetadatenId = newMeta.Id,
                MetadatenObjekt = newMeta,
                DokumentStatus = Status.Aktiv,
                IsUpdated = true
            };

            _db.Dokumente.Add(newDoc);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Datei kopiert: {newDoc.Dateiname} → {relativePath} (Kategorie: {finalKategorie})");

            return relativePath;
        }

        // 👉 Neue Methode nur für Versionen
        public async Task<string> CopyFileForVersionAsync(string sourcePath, string targetPath)
        {
            string cleanSource = sourcePath
                .Replace($"https://storage.googleapis.com/{_bucket}/", "")
                .Trim();

            if (string.IsNullOrWhiteSpace(cleanSource))
                throw new Exception($"Ungültiger Pfad: {sourcePath}");

            // 📂 Nur Kopieren in Firebase
            await _storage.CopyObjectAsync(_bucket, cleanSource, _bucket, targetPath);

            Console.WriteLine($"✅ Version gespeichert: {cleanSource} → {targetPath}");
            return targetPath;
        }
        // 🔹 Téléchargement d’un fichier depuis Firebase Storage
        public async Task<Stream> DownloadStreamAsync(string firebasePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firebasePath))
                    throw new ArgumentException("Firebase-Pfad darf nicht leer sein.", nameof(firebasePath));

                // Supprimer un éventuel préfixe "https://storage.googleapis.com/..."
                string cleanPath = firebasePath
                    .Replace($"https://storage.googleapis.com/{_bucket}/", "")
                    .Trim();

                Console.WriteLine($"📥 Lade Datei von Firebase: {cleanPath}");

                // Préparer un MemoryStream pour le contenu
                var memoryStream = new MemoryStream();

                // Télécharger depuis Firebase
                await _storage.DownloadObjectAsync(_bucket, cleanPath, memoryStream);

                // Revenir au début du stream
                memoryStream.Position = 0;

                Console.WriteLine($"✅ DownloadStreamAsync erfolgreich: {cleanPath}, Größe = {memoryStream.Length / 1024.0 / 1024.0:F2} MB");
                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim DownloadStreamAsync({firebasePath}): {ex.Message}");
                throw;
            }
        }




        public string GetPublicUrl(string objectPath)
        {
            return $"https://storage.googleapis.com/{_bucket}/{objectPath}";
        }

        // Uploader un document et retourner les URLs (direct et signée)
        public async Task<(string objectUrl, string objectName, string signedUrl)> UploadDocumentAsync(
      IFormFile file, string kategorie, string abteilung, string userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var firma = user?.FirmenName?.Trim();
            if (string.IsNullOrWhiteSpace(firma))
                throw new Exception("❌ FirmenName introuvable pour l'utilisateur.");

            // Normalisation
            string safeFirma = NormalizeSegment(firma);
            string safeAbteilung = NormalizeSegment(abteilung);
            string safeCategory = NormalizeSegment(kategorie);

            string objectName = $"dokumente/{safeFirma}/{safeAbteilung}/{safeCategory}/{file.FileName}";
            using var stream = file.OpenReadStream();
            await _storage.UploadObjectAsync(
                bucket: _bucket,
                objectName: objectName,
                contentType: DetermineContentType(file.FileName),
                source: stream
            );
            var signedUrl = GenerateSignedUrl(objectName, 15);
            return ($"https://storage.googleapis.com/{_bucket}/{objectName}", objectName, signedUrl ?? "");
        }



        // Détecter le type MIME
        private static string DetermineContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

        // Récupérer un objet
        public Google.Apis.Storage.v1.Data.Object? GetObject(string objectName)
        {
            try
            {
                return _storage.GetObject(_bucket, objectName);
            }
            catch
            {
                return null;
            }
        }
        public async Task<List<FolderItem>> ListFoldersAsync(string prefix)
        {
            var result = new HashSet<string>();
            prefix = EnsureTrailingSlash(prefix);

            await foreach (var obj in _storage.ListObjectsAsync(_bucket, prefix))
            {
                var relativePath = obj.Name.Substring(prefix.Length);
                var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 0)
                {
                    result.Add(segments[0]);
                }
            }

            return result
                .OrderBy(x => x)
                .Select(name => new FolderItem { Name = name, FullPath = $"{prefix}{name}" })
                .ToList(); 
        }


        public async Task RenameFolderAsync(string sourcePath, string targetPath)
        {
            await CopyFolderAsync(sourcePath, targetPath);
            await DeleteFolderAsync(sourcePath);
        }

        private string NormalizePath(string path)
        {
            return path?.Replace("\\", "/");
        }


        public async Task<Stream?> GetFileStreamAsync(string path)
        {
            try
            {
                // Pfad normalisieren (z. B. "dokumente/software/finanzen/versionen/Rechnung.pdf")
                var normalizedPath = Normalize(path);

                var memoryStream = new MemoryStream();

                // ✅ Richtiger Bucketname OHNE Leerzeichen!
                await _storage.DownloadObjectAsync(
                    bucket: "berbaze-4fbc8.appspot.com",
                    objectName: normalizedPath,
                    destination: memoryStream
                );

                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }
            catch (Exception ex)
            {
                // TODO: Logging einbauen
                Console.WriteLine($"❌ Fehler beim Download: {ex.Message}");
                return null;
            }
        }

        // Hilfsmethode für Normalisierung
        private string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            if (input.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                var idx = input.IndexOf('/', 5);
                if (idx > 0)
                    return input.Substring(idx + 1);
            }

            if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var idx = input.IndexOf('/', 30);
                if (idx > 0)
                    return input.Substring(idx + 1);
            }

            return input.TrimStart('/');
        }


        public async Task CopyFolderAsync(string sourcePath, string targetPath)
        {
            var sourcePrefix = EnsureTrailingSlash(sourcePath);
            var targetPrefix = EnsureTrailingSlash(targetPath);

            await foreach (var obj in _storage.ListObjectsAsync(_bucket, sourcePrefix))
            {
                using var mem = new MemoryStream();
                await _storage.DownloadObjectAsync(obj, mem);
                mem.Position = 0;
                var destName = obj.Name.Replace(sourcePrefix, targetPrefix);
                await _storage.UploadObjectAsync(_bucket, destName, obj.ContentType, mem);
            }
        }

        public async Task MoveFolderAsync(string sourcePath, string targetPath)
        {
            await RenameFolderAsync(sourcePath, targetPath);
        }

        public async Task<bool> DeleteFolderAsync(string folderPrefix)
        {
            var storageClient = StorageClient.Create();
            var bucketName = Bucket;

            var options = new ListObjectsOptions { PageSize = 1000 }; // z.B. 1000 pro Seite

            var objects = storageClient.ListObjects(bucketName, folderPrefix, options);
            foreach (var obj in objects)
            {
                await storageClient.DeleteObjectAsync(bucketName, obj.Name);
            }
            return true;
        }

        public async Task DeleteFileAsync(string filePath)
        {
            await _storage.DeleteObjectAsync(_bucket, filePath);
        }

        private static string NormalizeSegment(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "unbekannt";

            return input
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "-")   // remplace espaces par tiret
                .Replace("__", "_"); // évite doublons
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Le chemin est vide ou null.", nameof(path));

            return path.EndsWith("/") ? path : path + "/";
        }

        public async Task<List<DmsFile>> GetDocumentsByFolderAsync(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return new List<DmsFile>();

            folderPath = folderPath.TrimEnd('/') + "/"; // immer Slash am Ende

            var docs = new List<DmsFile>();

            // 🔹 Alle DB-Dokumente laden (für Metadaten + Abteilung)
            var dbDocs = await _db.Dokumente
                .Include(d => d.Abteilung)
                .ToListAsync();

            // ✅ Fix: Doppelte ObjectPaths zulassen -> wir nehmen einfach das erste Dokument
            var dbLookup = dbDocs
                .GroupBy(d => d.ObjectPath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            await foreach (var obj in _storage.ListObjectsAsync(_bucket, folderPath))
            {
                if (string.IsNullOrWhiteSpace(obj.Name) || obj.Name.EndsWith("/"))
                    continue; // nur Dateien

                dbLookup.TryGetValue(obj.Name, out var dbDoc);

                string abtName = null;
                if (dbDoc != null)
                {
                    if (dbDoc.Abteilung != null)
                        abtName = dbDoc.Abteilung.Name;
                    else if (dbDoc.AbteilungId != null)
                    {
                        var abt = await _db.Abteilungen.FindAsync(dbDoc.AbteilungId);
                        abtName = abt?.Name ?? "Allgemein";
                    }
                }

                var file = new DmsFile
                {
                    Id = dbDoc?.Id.ToString() ?? Guid.NewGuid().ToString(),
                    Name = Path.GetFileName(obj.Name),
                    Path = obj.Name,
                    SasUrl = GenerateSignedUrl(obj.Name, 15),
                    ObjectPath = obj.Name,
                    Kategorie = dbDoc?.Kategorie,
                    AbteilungName = abtName,
                    Beschreibung = dbDoc?.Beschreibung,
                    Titel = dbDoc?.Titel,
                    HochgeladenAm = dbDoc?.HochgeladenAm,
                    Status = dbDoc?.dtStatus.ToString(),
                    IsIndexed = dbDoc?.IsIndexed,
                    IsVersion = dbDoc?.IsVersion ?? false,
                    EstSigne = dbDoc?.EstSigne ?? false
                };

                docs.Add(file);
            }

            return docs.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<string> UploadWithMetadataAsync(
      Stream stream,
      string objectPath,
      string contentType,
      Metadaten? meta = null)
        {
            try
            {
                var metadata = new Dictionary<string, string>
                {
                    ["UploadedAt"] = DateTime.UtcNow.ToString("s"),
                    ["System"] = "DMS-Projekt"
                };

                if (meta != null)
                {
                    void Add(string key, object? value)
                    {
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                            metadata[key] = value.ToString();
                    }

                    // 🔹 Alle Felder deines Metadaten-Modells
                    Add("Titel", meta.Titel);
                    Add("Kategorie", meta.Kategorie);
                    Add("Rechnungsnummer", meta.Rechnungsnummer);
                    Add("Kundennummer", meta.Kundennummer);
                    Add("Rechnungsbetrag", meta.Rechnungsbetrag);
                    Add("Nettobetrag", meta.Nettobetrag);
                    Add("Gesamtpreis", meta.Gesamtpreis);
                    Add("Steuerbetrag", meta.Steuerbetrag);
                    Add("Rechnungsdatum", meta.Rechnungsdatum);
                    Add("Faelligkeitsdatum", meta.Faelligkeitsdatum);
                    Add("Zahlungsbedingungen", meta.Zahlungsbedingungen);
                    Add("Lieferart", meta.Lieferart);
                    Add("ArtikelAnzahl", meta.ArtikelAnzahl);
                    Add("Email", meta.Email);
                    Add("Telefon", meta.Telefon);
                    Add("IBAN", meta.IBAN);
                    Add("BIC", meta.BIC);
                    Add("Bankverbindung", meta.Bankverbindung);
                    Add("SteuerNr", meta.SteuerNr);
                    Add("UIDNummer", meta.UIDNummer);
                    Add("Adresse", meta.Adresse);
                }

                // 📤 Datei hochladen
                await _storage.UploadObjectAsync(
                    _bucket,
                    objectPath,
                    contentType ?? "application/octet-stream",
                    stream
                );

                // 🧠 Metadaten nachträglich setzen
                var obj = await _storage.GetObjectAsync(_bucket, objectPath);
                obj.Metadata = metadata;
                await _storage.UpdateObjectAsync(obj);

                Console.WriteLine($"✅ Datei mit Metadaten hochgeladen → {objectPath}");
                return $"https://storage.googleapis.com/{_bucket}/{objectPath}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler bei UploadWithMetadataAsync: {ex.Message}");
                throw;
            }
        }

        // ===============================================================
        // 🔹 SaveOrReuseAsync : Sauvegarde une nouvelle version de document
        // ===============================================================
        public async Task<(bool reused, string firebasePath, string hash)> SaveOrReuseAsync(
            Guid dokumentId,
            byte[] fileBytes,
            string? targetPath = null)
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new ArgumentException("Datei ist leer oder null.", nameof(fileBytes));

                // === 1️⃣ Calcul du hash (pour vérifier si le contenu existe déjà)
                using var ms = new MemoryStream(fileBytes);
                string hash = ComputeHash(ms);

                // === 2️⃣ Vérifie si un fichier avec le même hash existe déjà dans Firebase
                string hashFileName = $"{hash}.pdf";
                string defaultPath = targetPath ?? $"versionen/{hashFileName}";

                bool exists = await ObjectExistsAsync(defaultPath);
                if (exists)
                {
                    Console.WriteLine($"♻️ [Firebase] Datei bereits vorhanden → Wiederverwendung: {defaultPath}");
                    return (true, defaultPath, hash);
                }

                // === 3️⃣ Upload dans Firebase Storage
                string objectName = targetPath ?? defaultPath;

                using var uploadStream = new MemoryStream(fileBytes);
                await _storage.UploadObjectAsync(
                    bucket: _bucket,
                    objectName: objectName,
                    contentType: "application/pdf",
                    source: uploadStream
                );

                Console.WriteLine($"✅ [Firebase] Neue Version hochgeladen → {objectName}");

                return (false, objectName, hash);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler in SaveOrReuseAsync: {ex.Message}");
                throw;
            }
        }
        // 🔹 Permet de lister les objets d'un dossier Firebase
        public async IAsyncEnumerable<Google.Apis.Storage.v1.Data.Object> ListObjectsAsync(string prefix)
        {
            await foreach (var obj in _storage.ListObjectsAsync(_bucket, prefix))
            {
                yield return obj;
            }
        }


        /// <summary>
        /// 🔹 ComputeHash : SHA256-Hash d'un fichier (utilisé pour détecter si identique)
        /// </summary>
        public string ComputeHash(Stream stream)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();

            if (stream.CanSeek)
                stream.Position = 0;

            byte[] hashBytes = sha.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }



    }
}
