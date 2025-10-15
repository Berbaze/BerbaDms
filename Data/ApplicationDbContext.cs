using Azure;
using DmsProjeckt.Data;
using DocumentFormat.OpenXml.ExtendedProperties;
using MailKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Text;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace DmsProjeckt.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

        public DbSet<Kunden> Kunden { get; set; }
        public DbSet<Dokumente> Dokumente { get; set; }
        public DbSet<DokumentVersionen> DokumentVersionen { get; set; }
        public DbSet<Tags> Tags { get; set; }
        public DbSet<DokumentTags> DokumentTags { get; set; }
        public DbSet<DokumentRechte> DokumentRechte { get; set; }
        public DbSet<KundeBenutzer> KundeBenutzer { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Kommentare> Kommentare { get; set; }
        public DbSet<BenutzerMetadaten> BenutzerMetadaten { get; set; }
        public DbSet<AuditLogAdmin> AuditLogAdmins { get; set; }
        public DbSet<DashboardItem> DashboardItem { get; set; }
        public DbSet<UserDashboardItem> UserDashboardItem { get; set; }
        public DbSet<Notiz> Notiz { get; set; }
        public DbSet<Workflow> Workflows { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<Aufgaben> Aufgaben { get; set; }
        public DbSet<DokumentIndex> DokumentIndex { get; set; }
        public DbSet<AuditLogDokument> AuditLogDokumente { get; set; }
        public DbSet<StepKommentar> StepKommentare { get; set; }
        public DbSet<UserSharedDocument> UserSharedDocuments { get; set; }
        public DbSet<UserFavoritDokument> UserFavoritDokumente { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<NotificationType> NotificationTypes { get; set; }
        public DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }// Hinzugefügt für Gruppenchats
        public DbSet<UserSharedNote> UserSharedNotes { get; set; }

        public DbSet<SearchHistory> SearchHistory { get; set; }
        public DbSet<UserFavoritNote> UserFavoritNote { get; set; }
        public DbSet<Abteilung> Abteilungen { get; set; }
        public DbSet<MessageRead> MessageRead { get; set; } // Hinzugefügt für Lesebestätigungen
        public DbSet<SignatureRequest> SignatureRequests { get; set; }
        public DbSet<RecentHistory> RecentHistory { get; set; }
        public DbSet<FolderPermission> FolderPermissions { get; set; }
        public DbSet<DokumentSignatur> DokumentSignatur { get; set; }
        public DbSet<Archive> Archive { get; set; }        // ✅ Neu hinzugefügt
        public DbSet<Metadaten> Metadaten { get; set; }   // ✅ Neu hinzugefügt
        public DbSet<DuplicateUpload> DuplicateUploads { get; set; } // ✅ Neu hinzugefügt
        public DbSet<DokumentChunk> DokumentChunks { get; set; }
        public DbSet<DokumentVersionChunk> DokumentVersionChunks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ======================================================
            // 🧩 METADATEN-KONFIGURATION
            // ======================================================
            builder.Entity<Metadaten>(entity =>
            {
                // Dezimal-Präzision
                entity.Property(m => m.Rechnungsbetrag).HasPrecision(18, 2);
                entity.Property(m => m.Nettobetrag).HasPrecision(18, 2);
                entity.Property(m => m.Gesamtpreis).HasPrecision(18, 2);
                entity.Property(m => m.Steuerbetrag).HasPrecision(18, 2);

                // 🔗 1:1 Beziehung zwischen Metadaten ↔ Dokumente
                entity.HasOne(m => m.Dokument)
                      .WithOne(d => d.MetadatenObjekt)
                      .HasForeignKey<Metadaten>(m => m.DokumentId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .IsRequired(false); // erlaubt, dass Metadaten erst nach Dokument gespeichert werden
            });

            // ======================================================
            // 📄 DOKUMENTE
            // ======================================================
            builder.Entity<Dokumente>()
                .HasOne(d => d.ApplicationUser)
                .WithMany(u => u.Dokumente)
                .HasForeignKey(d => d.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Abteilung -> Dokumente (1:n)
            builder.Entity<Abteilung>()
                .HasMany(a => a.Dokumente)
                .WithOne(d => d.Abteilung)
                .HasForeignKey(d => d.AbteilungId)
                .OnDelete(DeleteBehavior.SetNull);

            // Versionen -> Dokumente (1:n)
            builder.Entity<DokumentVersionen>()
                .HasOne(dv => dv.Dokument)
                .WithMany(d => d.Versionen)
                .HasForeignKey(dv => dv.DokumentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Benutzer-Metadaten (Benutzerdefinierte Zusatzfelder)
            builder.Entity<BenutzerMetadaten>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Key).HasMaxLength(100);
                entity.Property(b => b.Value).HasMaxLength(500);

                entity.HasOne(b => b.Dokument)
                      .WithMany(d => d.BenutzerMetadaten)
                      .HasForeignKey(b => b.DokumentId);
            });

            // ======================================================
            // 🏷️ MANY-TO-MANY: Dokument <-> Tag
            // ======================================================
            builder.Entity<DokumentTags>()
                .HasKey(dt => new { dt.DokumentId, dt.TagId });

            builder.Entity<DokumentTags>()
                .HasOne(dt => dt.Dokument)
                .WithMany(d => d.DokumentTags)
                .HasForeignKey(dt => dt.DokumentId);

            builder.Entity<DokumentTags>()
                .HasOne(dt => dt.Tag)
                .WithMany(t => t.DokumentTags)
                .HasForeignKey(dt => dt.TagId);

            // ======================================================
            // 🧑‍💼 BENUTZER UND KUNDEN
            // ======================================================
            builder.Entity<Kunden>()
                .HasOne(k => k.ApplicationUser)
                .WithMany(u => u.Kunden)
                .HasForeignKey(k => k.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-Many Kunde <-> ApplicationUser
            builder.Entity<KundeBenutzer>()
                .HasKey(kb => new { kb.KundenId, kb.ApplicationUserId });

            builder.Entity<KundeBenutzer>()
                .HasOne(kb => kb.Kunden)
                .WithMany(k => k.KundeBenutzer)
                .HasForeignKey(kb => kb.KundenId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<KundeBenutzer>()
                .HasOne(kb => kb.ApplicationUser)
                .WithMany(u => u.KundenVerbindungen)
                .HasForeignKey(kb => kb.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================================================
            // 🧾 FAVORITEN UND FREIGABEN
            // ======================================================
            builder.Entity<UserFavoritDokument>()
                .HasKey(f => new { f.ApplicationUserId, f.DokumentId });

            builder.Entity<UserSharedDocument>()
                .HasOne(u => u.SharedToUser)
                .WithMany()
                .HasForeignKey(u => u.SharedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserFavoritNote>()
                .HasOne(f => f.Notiz)
                .WithMany()
                .HasForeignKey(f => f.NotizId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserSharedNote>()
                .HasOne(x => x.SharedByUser)
                .WithMany()
                .HasForeignKey(x => x.SharedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserSharedNote>()
                .HasOne(x => x.SharedToUser)
                .WithMany()
                .HasForeignKey(x => x.SharedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserSharedNote>()
                .HasOne(x => x.Notiz)
                .WithMany()
                .HasForeignKey(x => x.NotizId)
                .OnDelete(DeleteBehavior.Cascade);

            // ======================================================
            // 🧠 WORKFLOW & AUFGABEN
            // ======================================================
            builder.Entity<Workflow>()
                .HasMany(w => w.Steps)
                .WithOne(s => s.Workflow)
                .HasForeignKey(s => s.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Step>()
                .HasMany(s => s.Aufgaben)
                .WithOne(a => a.StepNavigation)
                .HasForeignKey(a => a.StepId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Aufgaben>()
                .HasOne(a => a.VonUserNavigation)
                .WithMany()
                .HasForeignKey(a => a.VonUser)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Aufgaben>()
                .HasOne(a => a.FuerUserNavigation)
                .WithMany()
                .HasForeignKey(a => a.FuerUser)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StepKommentar>(entity =>
            {
                entity.HasKey(sk => sk.Id);

                entity.HasOne(sk => sk.User)
                      .WithMany()
                      .HasForeignKey(sk => sk.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sk => sk.Step)
                      .WithMany(s => s.Kommentare)
                      .HasForeignKey(sk => sk.StepId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ======================================================
            // 💬 CHAT & NACHRICHTEN
            // ======================================================
            builder.Entity<ChatGroupMember>()
                .HasKey(cgm => new { cgm.ChatGroupId, cgm.UserId });

            builder.Entity<ChatGroupMember>()
                .HasOne(cgm => cgm.ChatGroup)
                .WithMany(g => g.ChatGroupMembers)
                .HasForeignKey(cgm => cgm.ChatGroupId);

            builder.Entity<ChatGroupMember>()
                .HasOne(cgm => cgm.User)
                .WithMany(u => u.ChatGroupMembers)
                .HasForeignKey(cgm => cgm.UserId);

            builder.Entity<MessageRead>()
                .HasKey(m => new { m.MessageId, m.UserId });

            // ======================================================
            // 📚 DASHBOARD & BERECHTIGUNGEN
            // ======================================================
            builder.Entity<UserDashboardItem>()
                .HasOne(udi => udi.DashboardItem)
                .WithMany(di => di.UserDashboardItems)
                .HasForeignKey(udi => udi.DashboardItemId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FolderPermission>()
                .HasOne(fp => fp.User)
                .WithMany()
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FolderPermission>()
                .HasOne(fp => fp.GrantedByAdmin)
                .WithMany()
                .HasForeignKey(fp => fp.GrantedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // ======================================================
            // 🔍 SUCHVERLAUF
            // ======================================================
            builder.Entity<SearchHistory>()
                .HasOne(sh => sh.Dokument)
                .WithMany()
                .HasForeignKey(sh => sh.DokumentId);

            builder.Entity<DokumentChunk>()
     .HasOne(c => c.Dokument)
     .WithMany(d => d.Chunks)
     .HasForeignKey(c => c.DokumentId)
     .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Version ↔ Chunk (n : n)
            builder.Entity<DokumentVersionChunk>()
                .HasKey(vc => new { vc.VersionId, vc.ChunkId });

            builder.Entity<DokumentVersionChunk>()
                .HasOne(vc => vc.Version)
                .WithMany(v => v.VersionChunks)
                .HasForeignKey(vc => vc.VersionId);

            builder.Entity<DokumentVersionChunk>()
                .HasOne(vc => vc.Chunk)
                .WithMany(c => c.VersionChunks)
                .HasForeignKey(vc => vc.ChunkId);
        }




    }
}
