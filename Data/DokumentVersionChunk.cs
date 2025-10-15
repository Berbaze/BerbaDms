using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class DokumentVersionChunk
    {
        [Key, Column(Order = 0)]
        public int VersionId { get; set; }

        [Key, Column(Order = 1)]
        public Guid ChunkId { get; set; }

        [ForeignKey(nameof(VersionId))]
        public DokumentVersionen Version { get; set; } = null!;

        [ForeignKey(nameof(ChunkId))]
        public DokumentChunk Chunk { get; set; } = null!;
    }
}
