using DmsProjeckt.Data; // falls deine Entities dort liegen
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Service
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            Console.WriteLine("🚀 Starte Datenbank-Seeding...");

            // 🔹 Stelle sicher, dass DB existiert
            await context.Database.EnsureCreatedAsync();

            // ============================
            // 🟢 Dashboard Widgets
            // ============================
            var dashboardItems = new[]
            {
                new DashboardItem { Title = "Favoriten", Icon = "&#11088;", Nail = "favoriten", Beschreibung = "Schnellzugriff auf deine bevorzugten Dokumente und Inhalte" },
                new DashboardItem { Title = "Archiv", Icon = "&#128452;", ActionLink="/Dokument/Index?typ=archiviert", Nail = "archiv", Beschreibung = "Langfristige Ablage von inaktiven oder abgeschlossenen Dokumenten" },
                new DashboardItem { Title = "Versionen", Icon = "&#128209;", ActionLink="/Dokument/AlleVersionen", Nail = "versionen", Beschreibung = "Übersicht und Wiederherstellung früherer Dokumentversionen" },
                new DashboardItem { Title = "Aufgaben", Icon = "&#9989;", ActionLink="/Tests/Aufgaben", Nail = "aufgaben", Beschreibung = "Verwalte deine persönlichen oder zugewiesenen Aufgaben" },
                new DashboardItem { Title = "Scannen", Icon = "&#128224;", Nail = "scannen", Beschreibung = "Dokumente direkt ins System einscannen" },
                new DashboardItem { Title = "Suche", Icon = "&#128269;", ActionLink="/Dokument/Suchen", Nail = "suche", Beschreibung = "Durchsuche alle Dokumente und Inhalte im System" },
                new DashboardItem { Title = "Geteilte Inhalte", Icon = "&#129309;", ActionLink="/GeteilteDokumente", Nail = "geteilt", Beschreibung = "Anzeigen von Dokumenten, die du mit anderen geteilt hast oder erhalten hast" },
                new DashboardItem { Title = "Notizen", Icon = "&#128221;", ActionLink="/Notiz/Index", Nail = "notizen", Beschreibung = "Erstelle und verwalte persönliche oder geteilte Notizen" },
                new DashboardItem { Title = "Zuletzt bearbeitet", Icon = "&#128338;", ActionLink="/AuditLog", Nail = "zuletzt", Beschreibung = "Zeigt kürzlich geänderte oder bearbeitete Inhalte" },
                new DashboardItem { Title = "Import", Icon = "&#128190;", ActionLink="/Tests/UploadMulti", Nail = "import", Beschreibung = "Dokumente oder Dateien ins System importieren" },
                new DashboardItem { Title = "Dokumente", Icon = "&#128193;", ActionLink="/Dokument/Index", Nail = "ablage", Beschreibung = "Verwalten Sie Ihre Dokumente" },
                new DashboardItem { Title = "Signatur", Icon = "&#9997;&#65039;", ActionLink="/Pdf/Edit", Nail = "signatur", Beschreibung = "Dokumente digital unterschreiben und freigeben" },
                new DashboardItem { Title = "Workflows", Icon = "&#128736;", ActionLink="/Workflows/Index", Nail = "workflow", Beschreibung = "Statusübersicht deiner Arbeitsprozesse und Freigabeschritte" },
                new DashboardItem { Title = "Adminverwaltung", Icon = "&#128101;", ActionLink="/Dokument/DashboardAdmin", Nail = "admin", Beschreibung = "Verwalten und Überwachen Sie Ihre Nutzer" },
                new DashboardItem { Title = "Nachrichten", Icon = "&#128488;", ActionLink="/Chat/Chat", Nail = "chat", Beschreibung = "Tauschen Sie sich mit Ihren Kollegen und Kolleginnen aus" }
            };

            foreach (var item in dashboardItems)
            {
                if (!await context.DashboardItem.AnyAsync(x => x.Nail == item.Nail))
                    await context.DashboardItem.AddAsync(item);
            }

            // ============================
            // 🟢 Notifications
            // ============================
            var notifications = new[]
            {
                new NotificationType { Name = "Erstellt", Description = "Aufgabe erstellt" },
                new NotificationType { Name = "ErstelltEmail", Description = "Aufgabe erstellt Email" },
                new NotificationType { Name = "Erledigt", Description = "Aufgabe erledigt" },
                new NotificationType { Name = "ErledigtEmail", Description = "Aufgabe erledigt email" },
                new NotificationType { Name = "Workflowaufgabe", Description = "Workflowaufgabe erstellt" },
                new NotificationType { Name = "Workflowaufgabe Email", Description = "Workflowaufgabe erstellt email" },
                new NotificationType { Name = "Workflow erledigt", Description = "Workflowaufgabe erledigt" },
                new NotificationType { Name = "Workflow erledigt Email", Description = "Workflowaufgabe erledigt email" },
                new NotificationType { Name = "Workflow done", Description = "Workflow abgeschlossen" },
                new NotificationType { Name = "Workflow done Email", Description = "Workflow abgeschlossen Email" },
                new NotificationType { Name = "Doc shared", Description = "Dokument geteilt" },
                new NotificationType { Name = "Doc shared Email", Description = "Doc shared Email" },
                new NotificationType { Name = "Note shared", Description = "Notiz geteilt" },
                new NotificationType { Name = "Note shared email", Description = "Notiz geteilt email" },
                new NotificationType { Name = "Due", Description = "Aufgabe fällig" },
                new NotificationType { Name = "Due email", Description = "Aufgabe fällig email" },
                new NotificationType { Name = "DueWF", Description = "Workflowaufgabe fällig" },
                new NotificationType { Name = "DueWFEmail", Description = "Workflowaufgabe fällig email" },
                new NotificationType { Name = "SignRq", Description = "Signatur angefragt" },
                new NotificationType { Name = "SignRqEm", Description = "Signatur angefragt Email" },
                new NotificationType { Name = "SignRqDone", Description = "Signiert" },
                new NotificationType { Name = "SignRqDoneEm", Description = "Signiert done email" }
            };

            foreach (var n in notifications)
            {
                if (!await context.NotificationTypes.AnyAsync(x => x.Name == n.Name))
                    await context.NotificationTypes.AddAsync(n);
            }

            // ============================
            // 🏢 Abteilungen hinzufügen
            // ============================
            var abteilungen = new[]
            {
                "Versionen", "GrossDoc", "Archiv", "UnterlagenSigniert", "gescannteDokumente",
                "Finanzen", "IT", "Logistik", "Recht", "Technik", "Verkauf",
                "Allgemein", "Marketing", "Verwaltung", "Projektmanagement", "HR",
                "Qualität", "Support", "Management", "Einkauf", "Studium", "Forschung",
                "Kommunikation", "Produktion", "Administration", "Sonstige", "gescannte-dokumente"
            };

            foreach (var name in abteilungen)
            {
                if (!await context.Abteilungen.AnyAsync(a => a.Name == name))
                {
                    await context.Abteilungen.AddAsync(new Abteilung
                    {
                        Name = name,
                    });
                }
            }

            // ============================
            // 💾 Alles speichern
            // ============================
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Datenbank-Seeding abgeschlossen!");
        }
    }
}
