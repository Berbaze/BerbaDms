using System.Security.Cryptography;
using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Service
{
    public class DocumentHashService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly FirebaseStorageService _firebase;

        public DocumentHashService(ApplicationDbContext dbContext, FirebaseStorageService firebase)
        {
            _dbContext = dbContext;
            _firebase = firebase;
        }

        /// <summary>
        /// Calcule le hash SHA256 d’un flux de fichier
        /// </summary>
        public string ComputeHash(Stream fileStream)
        {
            using var sha = SHA256.Create();
            fileStream.Position = 0;
            var hashBytes = sha.ComputeHash(fileStream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Vérifie si un fichier avec le même hash existe déjà
        /// </summary>
        public async Task<Dokumente?> FindExistingAsync(string fileHash)
        {
            return await _dbContext.Dokumente.FirstOrDefaultAsync(d => d.FileHash == fileHash);
        }

        /// <summary>
        /// Sauvegarde ou réutilise un fichier selon son hash
        /// </summary>
        public async Task<(bool reused, string firebasePath, string hash)> SaveOrReuseAsync(
            Guid dokumentId, byte[] fileBytes)
        {
            var hash = ComputeHash(new MemoryStream(fileBytes));
            var existing = await FindExistingAsync(hash);

            if (existing != null)
            {
                Console.WriteLine($"♻️ Fichier déjà présent : {existing.ObjectPath}");
                return (true, existing.ObjectPath, hash);
            }

            // 🔄 Sinon, upload vers Firebase
            string path = $"Dokumente/{dokumentId}_v{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            using var uploadStream = new MemoryStream(fileBytes);
            await _firebase.UploadStreamAsync(uploadStream, path, "application/pdf");

            return (false, path, hash);
        }
    }
}
