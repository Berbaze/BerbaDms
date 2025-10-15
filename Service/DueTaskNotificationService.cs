using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DmsProjeckt.Data;

namespace DmsProjeckt.Service
{
    public class DueTaskNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        
        public DueTaskNotificationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Intervall für die Prüfung (z.B. jede Minute)
            var checkInterval = TimeSpan.FromMinutes(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Zeitbasis: IMMER UTC verwenden!
                    var now = DateTime.UtcNow;

                    // NotificationType Ids für fällige Aufgaben
                    var faelligTypeIds = await context.NotificationTypes
                        .Where(nt => nt.Name == "Due" || nt.Name == "DueWF" || nt.Name == "Due email" || nt.Name == "DueWFEmail")
                        .Select(nt => nt.Id)
                        .ToListAsync(stoppingToken);

                    // Alle Settings, die aktiviert sind und AdvanceMinutes gesetzt haben
                    var settings = await context.UserNotificationSettings
                        .Where(s => faelligTypeIds.Contains(s.NotificationTypeId) && s.Enabled)
                        .ToListAsync(stoppingToken);

                    // Maximalen Vorlauf bestimmen, um Fenster zu berechnen
                    int advanceMax = settings.Count > 0 ? settings.Max(s => s.AdvanceMinutes ?? 60) : 60;
                    var windowStart = now;
                    var windowEnd = now.AddMinutes(advanceMax);

                    // Hole alle offenen Aufgaben, deren Fälligkeitszeit im Prüfungsfenster liegt
                    var offeneAufgaben = await context.Aufgaben
                        .Where(a => a.FaelligBis > windowStart
                                    && a.FaelligBis <= windowEnd
                                    && !a.Erledigt
                                    && a.Aktiv)
                        .ToListAsync(stoppingToken);

                    // Debug-Ausgabe
                    Console.WriteLine($"[DueTaskNotification] {DateTime.UtcNow}: Prüfe {offeneAufgaben.Count} Aufgaben im Zeitfenster {windowStart} bis {windowEnd}");

                    foreach (var aufgabe in offeneAufgaben)
                    {
                        foreach (var typeId in faelligTypeIds)
                        {
                            var userSetting = settings.FirstOrDefault(
                                s => s.UserId == aufgabe.FuerUser && s.NotificationTypeId == typeId);

                            if (userSetting == null) continue;

                            int advance = userSetting.AdvanceMinutes ?? 60;
                            DateTime notifyAt = aufgabe.FaelligBis.AddMinutes(-advance);

                            // Logging
                            Console.WriteLine($"[DueTaskNotification] Aufgabe: {aufgabe.Titel}, Fällig: {aufgabe.FaelligBis:yyyy-MM-dd HH:mm}, Advance: {advance}, NotifyAt: {notifyAt:yyyy-MM-dd HH:mm}, Now: {now:yyyy-MM-dd HH:mm}");

                            // Nur einmal senden: gibt es schon eine Notification zu dieser Aufgabe & Typ?
                            bool alreadySent = await context.UserNotifications
    .Include(un => un.Notification)
    .AnyAsync(un =>
        un.UserId == aufgabe.FuerUser &&
        un.Notification.NotificationTypeId == typeId &&
        un.Notification.Content == $"Die Aufgabe \"{aufgabe.Titel}\" ist fällig am {aufgabe.FaelligBis:g}.",
        stoppingToken
    );


                            if (alreadySent)
                            {
                                Console.WriteLine($"[DueTaskNotification] -> Benachrichtigung für Aufgabe '{aufgabe.Titel}' bereits gesendet.");
                                continue;
                            }

                            // Zeitpunkt erreicht?
                            if (notifyAt <= now && aufgabe.FaelligBis > now)
                            {
                                var notificationTitle = (typeId == faelligTypeIds[0] || typeId == faelligTypeIds[2])
                                    ? "Aufgabe fällig"
                                    : "Workflowaufgabe fällig";

                                var notification = new Notification
                                {
                                    Title = notificationTitle,
                                    Content = $"Die Aufgabe \"{aufgabe.Titel}\" ist fällig am {aufgabe.FaelligBis:g}.",
                                    CreatedAt = DateTime.UtcNow,
                                    NotificationTypeId = typeId
                                };
                                context.Notifications.Add(notification);
                                await context.SaveChangesAsync(stoppingToken);

                                var userNotification = new UserNotification
                                {
                                    UserId = aufgabe.FuerUser,
                                    NotificationId = notification.Id,
                                    IsRead = false,
                                    ReceivedAt = DateTime.UtcNow,
                                    SendAt = DateTime.UtcNow
                                };
                                context.UserNotifications.Add(userNotification);
                                await context.SaveChangesAsync(stoppingToken);

                                Console.WriteLine($"[DueTaskNotification] -> Benachrichtigung für Aufgabe '{aufgabe.Titel}' an User {aufgabe.FuerUser} erstellt.");
                                // Optional: Sende auch Email etc.
                               
                            }
                        }
                    }
                }

                await Task.Delay(checkInterval, stoppingToken);
            }
        }
    }
}
