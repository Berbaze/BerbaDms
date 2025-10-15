using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Google.Apis.Storage.v1.Data;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading; 

namespace DmsProjeckt.Service
{
    public class ChunkService
    {
        private readonly ApplicationDbContext _db;
        private readonly FirebaseStorageService _firebase;
        private const int CHUNK_SIZE = 20 * 1024 * 1024; // 🔹 20 MB
        private readonly string _bucket = "dokumente"; // ou ton nom réel de bucket


        public ChunkService(ApplicationDbContext db, FirebaseStorageService firebase)
        {
            _db = db;
            _firebase = firebase;
        }

        // 🧩 1️⃣ Sauvegarder un fichier en chunks sur Firebase
        public async Task<List<DokumentChunk>> SaveFileAsChunksToFirebaseAsync(
            Stream fileStream,
            Guid dokumentId,
            string firma,
            string abteilung,
            string kategorie)
        {
            // 🔹 Normalisation → évite "Berba" / "berba"
            string safeFirma = (firma ?? "unbekannt").Trim().ToLowerInvariant();
            string safeAbteilung = (abteilung ?? "allgemein").Trim().ToLowerInvariant();
            string safeKategorie = (kategorie ?? "allgemein").Trim().ToLowerInvariant();

            var chunks = new List<DokumentChunk>();
            int index = 0;
            byte[] buffer = new byte[CHUNK_SIZE];

            Console.WriteLine($"📦 [Start Chunk Upload] Firma={safeFirma}, Abt={safeAbteilung}, Kat={safeKategorie}");

            while (true)
            {
                int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                var chunkData = buffer.Take(bytesRead).ToArray();
                string hash = ComputeSha256(chunkData);

                // 🔹 Vérifie si le chunk existe déjà
                var existing = await _db.DokumentChunks.FirstOrDefaultAsync(c => c.Hash == hash);
                if (existing != null)
                {
                    Console.WriteLine($"♻️ Chunk {index} réutilisé (hash match)");
                    chunks.Add(existing);
                    index++;
                    continue;
                }

                // ✅ Nouveau chemin complet : dokumente/firma/abteilung/kategorie/chunks/{dokId}/chunk_x.bin
                string firebasePath = $"dokumente/{safeFirma}/{safeAbteilung}/{safeKategorie}/chunks/{dokumentId}/chunk_{index}.bin";

                try
                {
                    using var ms = new MemoryStream(chunkData);
                    Console.WriteLine($"📤 Uploading chunk_{index}.bin → {firebasePath} ({bytesRead / 1024.0 / 1024.0:F2} MB)");
                    await _firebase.UploadStreamAsync(ms, firebasePath, "application/octet-stream");
                    Console.WriteLine($"✅ Upload terminé → {firebasePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur Upload chunk_{index}.bin → {firebasePath}: {ex.Message}");
                    throw;
                }

                var chunk = new DokumentChunk
                {
                    Id = Guid.NewGuid(),
                    DokumentId = dokumentId,
                    Index = index,
                    Hash = hash,
                    Size = bytesRead,
                    FirebasePath = firebasePath,
                    CreatedAt = DateTime.UtcNow
                };

                _db.DokumentChunks.Add(chunk);
                chunks.Add(chunk);
                index++;
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"✅ {chunks.Count} Chunks enregistrés en base et uploadés sur Firebase.");
            return chunks;
        }
        public async Task<string?> ReconstructFileFromFirebaseAsync(Guid dokumentId)
        {
            // 🧹 Nettoyage automatique avant toute reconstruction
            CleanOldTempFiles();

            var dokument = await _db.Dokumente
                .Include(d => d.Abteilung)
                .FirstOrDefaultAsync(d => d.Id == dokumentId);

            if (dokument == null)
            {
                Console.WriteLine($"❌ Dokument {dokumentId} introuvable.");
                return null;
            }

            if (!dokument.IsChunked)
            {
                Console.WriteLine($"ℹ️ Dokument {dokumentId} n'est pas chunked → renvoi direct du chemin complet.");
                return dokument.ObjectPath;
            }

            // 🔍 Extraire l’ID du dossier chunk sans slashs superflus
            var chunkedFolderId = dokument.ObjectPath
                .Replace("chunked:/", "")
                .Replace("chunked://", "")
                .Trim('/');

            // 📁 Construire le chemin dynamique selon la vraie Abteilung et la catégorie
            var firma = "berba"; // 🏢 À adapter si tu gères plusieurs sociétés
            var abteilungName = dokument.Abteilung?.Name?.ToLowerInvariant() ?? "allgemein";
            var kategorie = dokument.Kategorie?.ToLowerInvariant() ?? "allgemein";

            var chunkFolderPath = $"dokumente/{firma}/{abteilungName}/{kategorie}/chunks/{chunkedFolderId}";
            Console.WriteLine($"📁 Lecture des chunks depuis: {chunkFolderPath}");

            // 📦 Préparer le fichier temporaire de sortie
            var tempDir = Path.Combine(Path.GetTempPath(), "DMS_Reconstructed");
            Directory.CreateDirectory(tempDir);

            var safeFileName = string.Join("_", dokument.Dateiname.Split(Path.GetInvalidFileNameChars()));
            var tempFilePath = Path.Combine(tempDir, safeFileName);

            // ✅ Libérer le fichier avant recréation
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Impossible de supprimer le cache existant : {ex.Message}");
                }
            }

            await using var output = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            int index = 0;
            while (true)
            {
                var chunkPath = $"{chunkFolderPath}/chunk_{index}.bin";
                try
                {
                    Console.WriteLine($"📥 Téléchargement du chunk : {chunkPath}");
                    using var chunkStream = await _firebase.DownloadStreamAsync(chunkPath);
                    await chunkStream.CopyToAsync(output);
                    Console.WriteLine($"✅ Chunk {index} ajouté ({chunkPath})");
                    index++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ℹ️ Fin de lecture des chunks (dernier index = {index - 1}) → {ex.Message}");
                    break;
                }
            }

            await output.FlushAsync();

            if (File.Exists(tempFilePath))
                Console.WriteLine($"✅ Fichier reconstruit: {tempFilePath}");
            else
                Console.WriteLine($"❌ Erreur : le fichier temporaire n’a pas été créé.");

            return tempFilePath;
        }
        private static readonly SemaphoreSlim _reconstructLock = new(1, 1);

        public async Task<string> EnsureFileReconstructedAsync(Guid dokumentId)
        {
            await _reconstructLock.WaitAsync();
            try
            {
                var doc = await _db.Dokumente
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == dokumentId);

                if (doc == null || string.IsNullOrWhiteSpace(doc.ObjectPath))
                    throw new Exception("📄 Dokument introuvable ou chemin invalide.");

                // 🧩 Dossier temporaire unique par session (empêche les verrous Windows)
                string sessionDir = Path.Combine(Path.GetTempPath(), "DMS_Reconstructed_Sessions", Guid.NewGuid().ToString());
                Directory.CreateDirectory(sessionDir);

                string fileNameBase = Path.GetFileNameWithoutExtension(doc.Dateiname);
                string reconstructedPath = Path.Combine(sessionDir, $"{fileNameBase}.pdf");

                Console.WriteLine($"📂 Dossier temporaire de session : {sessionDir}");

                // 🔍 Base path Firebase
                var basePath = doc.ObjectPath
                    .Replace("chunked://", "")
                    .Trim()
                    .TrimEnd('/');

                Console.WriteLine($"🔍 Lecture des chunks depuis : {basePath}");

                // 📥 Liste des chunks depuis Firebase
                var chunkObjects = new List<Google.Apis.Storage.v1.Data.Object>();
                await foreach (var obj in _firebase.ListObjectsAsync($"{basePath}/"))
                {
                    chunkObjects.Add(obj);
                }

                if (!chunkObjects.Any())
                    throw new Exception($"❌ Aucun chunk trouvé pour {basePath}");

                // 🧩 Reconstruction locale du PDF
                using (var output = new FileStream(reconstructedPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    int index = 0;
                    foreach (var chunk in chunkObjects.OrderBy(c => c.Name))
                    {
                        try
                        {
                            Console.WriteLine($"⬇️ Téléchargement du chunk : {chunk.Name}");
                            using (var stream = await _firebase.DownloadStreamAsync(chunk.Name))
                            {
                                await stream.CopyToAsync(output);
                            }
                            Console.WriteLine($"✅ Chunk {index} ajouté ({chunk.Name})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Chunk manquant ({chunk.Name}) : {ex.Message}");
                            break;
                        }
                        index++;
                    }
                }

                // ⏳ Vérifie que le fichier est bien libéré avant de le lire
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        using var fs = new FileStream(reconstructedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        break; // succès : fichier libéré
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("⏳ Le fichier est encore verrouillé, attente 500ms...");
                        await Task.Delay(500);
                    }
                }

                // ✅ Validation du PDF reconstruit
                if (!File.Exists(reconstructedPath))
                    throw new Exception("❌ Fichier non trouvé après reconstruction !");

                await using (var fs = new FileStream(reconstructedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var buffer = new byte[5];
                    await fs.ReadAsync(buffer, 0, 5);
                    string header = System.Text.Encoding.ASCII.GetString(buffer);

                    if (!header.StartsWith("%PDF"))
                    {
                        File.Delete(reconstructedPath);
                        throw new Exception("❌ Le fichier reconstruit n'est pas un PDF valide !");
                    }
                }

                Console.WriteLine($"✅ Fichier PDF valide reconstruit : {reconstructedPath}");

                // 🧹 Nettoyage automatique du dossier de session après 2 minutes
                _ = Task.Run(async () =>
                {
                    await Task.Delay(120000); // 2 minutes
                    try
                    {
                        if (Directory.Exists(sessionDir))
                        {
                            Directory.Delete(sessionDir, true);
                            Console.WriteLine($"🧹 Dossier temporaire supprimé : {sessionDir}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Impossible de supprimer {sessionDir} : {ex.Message}");
                    }
                });

                return reconstructedPath;
            }
            finally
            {
                _reconstructLock.Release();
            }
        }



        private void CleanOldTempFiles()
        {
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "DMS_Reconstructed");
                if (!Directory.Exists(tempDir))
                    return;

                var files = Directory.GetFiles(tempDir);
                foreach (var file in files)
                {
                    var info = new FileInfo(file);

                    // 🧹 Supprimer fichiers vides ou plus vieux que 24h
                    if (info.Length == 0 || info.CreationTimeUtc < DateTime.UtcNow.AddHours(-24))
                    {
                        Console.WriteLine($"🧹 Suppression du cache corrompu/ancien : {info.Name}");
                        try
                        {
                            info.Delete();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Impossible de supprimer {info.Name} : {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erreur CleanOldTempFiles : {ex.Message}");
            }
        }



        // ♻️ 3️⃣ Comparer deux ensembles de chunks (versionnage)
        public async Task<List<DokumentChunk>> CompareAndUploadNewVersionChunksAsync(
            Guid oldDokumentId,
            Stream newFileStream,
            Guid newDokumentId,
            string firma,
            string abteilung,
            string kategorie)
        {
            string safeFirma = (firma ?? "unbekannt").Trim().ToLowerInvariant();
            string safeAbteilung = (abteilung ?? "allgemein").Trim().ToLowerInvariant();
            string safeKategorie = (kategorie ?? "allgemein").Trim().ToLowerInvariant();

            var oldChunks = await _db.DokumentChunks
                .Where(c => c.DokumentId == oldDokumentId)
                .OrderBy(c => c.Index)
                .ToListAsync();

            var newChunks = new List<DokumentChunk>();
            int index = 0;
            byte[] buffer = new byte[CHUNK_SIZE];

            while (true)
            {
                int bytesRead = await newFileStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                var chunkData = buffer.Take(bytesRead).ToArray();
                string hash = ComputeSha256(chunkData);

                var existing = oldChunks.FirstOrDefault(c => c.Hash == hash);
                if (existing != null)
                {
                    Console.WriteLine($"♻️ Chunk {index} inchangé → réutilisé");
                    var reused = new DokumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DokumentId = newDokumentId,
                        Index = index,
                        Hash = hash,
                        Size = existing.Size,
                        FirebasePath = existing.FirebasePath,
                        CreatedAt = DateTime.UtcNow
                    };
                    newChunks.Add(reused);
                    _db.DokumentChunks.Add(reused);
                    index++;
                    continue;
                }

                // 🔹 Nouveau chunk → upload Firebase
                string firebasePath = $"dokumente/{safeFirma}/{safeAbteilung}/{safeKategorie}/chunks/{newDokumentId}/chunk_{index}.bin";

                try
                {
                    using var ms = new MemoryStream(chunkData);
                    Console.WriteLine($"📤 Uploading new chunk_{index}.bin → {firebasePath}");
                    await _firebase.UploadChunkAsync(ms, firebasePath);
                    Console.WriteLine($"✅ Upload terminé → {firebasePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erreur Upload chunk_{index}.bin → {firebasePath}: {ex.Message}");
                    throw;
                }

                var newChunk = new DokumentChunk
                {
                    Id = Guid.NewGuid(),
                    DokumentId = newDokumentId,
                    Index = index,
                    Hash = hash,
                    Size = bytesRead,
                    FirebasePath = firebasePath,
                    CreatedAt = DateTime.UtcNow
                };

                _db.DokumentChunks.Add(newChunk);
                newChunks.Add(newChunk);
                index++;
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"✅ {newChunks.Count} nouveaux chunks enregistrés pour la version {newDokumentId}");
            return newChunks;
        }




        // Utilitaire : calcul SHA256
        private string ComputeSha256(byte[] data)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLower();
        }
    }
}
