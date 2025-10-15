using System.ComponentModel.DataAnnotations;

namespace DmsProjeckt.Data
{
    public class DmsFolder
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string? ParentPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<DmsFolder> SubFolders { get; set; } = new();
        public List<DmsFile> Files { get; set; } = new();

        public bool IsAbteilung { get; set; }
        public string Icon { get; set; } = "folder";

        // 🔹 Infos additionnelles
        public string? Beschreibung { get; set; }
        public string? Kategorie { get; set; }
        public string? Titel { get; set; }

        // 🧠 NEU: Lien vers Metadaten
        public Metadaten? MetadatenObjekt { get; set; }

        // 🗃️ Optionnel : compteur de fichiers
        public int DateiAnzahl => (Files?.Count ?? 0) + (SubFolders?.Sum(sf => sf.Files?.Count ?? 0) ?? 0);
    }
}
