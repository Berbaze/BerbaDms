using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DmsProjeckt.Data;

namespace DmsProjeckt.Data
{
    public class Step
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }

        public string Description { get; set; }

        public int Order { get; set; }
        public string? UserId { get; set; }

        // FK Beziehung zu Workflow
        public int? WorkflowId { get; set; }

        [ForeignKey("WorkflowId")]
        public Workflow? Workflow { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? AssignedToUser { get; set; }
        public DateTime? DueDate { get; set; }
        public ICollection<Aufgaben>? Aufgaben { get; set; }
        public bool Completed { get; set; }
        public bool TaskCreated { get; set; }
        public string Kategorie { get; set; } = string.Empty;
        public virtual List<StepKommentar> Kommentare { get; set; }
        public virtual List<Dokumente> Dokumente { get; set; }

    }
}