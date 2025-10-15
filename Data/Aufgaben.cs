using DmsProjeckt.Data;

namespace DmsProjeckt.Data
{
    public class Aufgaben
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Titel { get; set; }
        public string? Beschreibung { get; set; }
        public DateTime FaelligBis { get; set; }
        public int Prioritaet { get; set; }
        public bool Erledigt { get; set; }
        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        public string? VonUser { get; set; }
        public string? FuerUser { get; set; }

        public ApplicationUser? VonUserNavigation { get; set; }
        public ApplicationUser? FuerUserNavigation { get; set; }
        public ICollection<Dokumente>? Dateien { get; set; }
        public int? StepId { get; set; }
        public Step? StepNavigation { get; set; }
        public bool Aktiv { get; set; }
        public int? WorkflowId { get; set; }
        public Workflow? Workflow { get; set; }
    }
}
