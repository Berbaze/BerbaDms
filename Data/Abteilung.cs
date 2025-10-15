namespace DmsProjeckt.Data
{
    public class Abteilung
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Relation 1:n avec Dokument
        public ICollection<Dokumente> Dokumente { get; set; }
    }
}
