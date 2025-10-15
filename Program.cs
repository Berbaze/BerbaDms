using DinkToPdf;
using DinkToPdf.Contracts;
using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Hubs;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

// ==============================
// 🔧 WebApp-Konfiguration
// ==============================
var builder = WebApplication.CreateBuilder(args);

var jsonKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "berbaze-4fbc8-firebase-adminsdk-q5suu-9e6ca59e32.json");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 524_288_000; // 500 MB
    options.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http1);
});

// ==============================
// 🔌 DB + Identity
// ==============================
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ==============================
// 🔹 Dependency Injection Services
// ==============================
builder.Services.AddScoped<LocalIndexService>();
builder.Services.AddScoped<AzureOcrService>();
builder.Services.AddScoped<PdfSigningService>();
builder.Services.AddScoped<VersionierungsService>();
builder.Services.AddScoped<AdminAuditService>();
builder.Services.AddScoped<AuditLogDokumentService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<DokumentIndexService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<FirebaseStorageService>();
builder.Services.AddHostedService<DueTaskNotificationService>();
builder.Services.AddScoped<DocumentHashService>();
builder.Services.AddScoped<ChunkService>(); 

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton(new MyFirebaseOptions
{
    Bucket = configuration["Firebase:Bucket"],
    AuthToken = configuration["Firebase:AuthToken"]
});

builder.Services.AddSingleton(provider =>
{
    var credential = GoogleCredential
        .FromFile(jsonKeyPath)
        .CreateScoped(StorageService.Scope.DevstorageFullControl);

    return new StorageService(new BaseClientService.Initializer
    {
        HttpClientInitializer = credential,
        ApplicationName = "DmsProjeckt"
    });
});

// ==============================
// 🔹 PDF + Razor Rendering
// ==============================
builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
builder.Services.AddSingleton<IConverter, SynchronizedConverter>(_ =>
    new SynchronizedConverter(new PdfTools()));

builder.Services.AddSingleton<PdfMetadataReader>();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524_288_000; // 500 MB
});

// ==============================
// 🔹 Sessions + TempData
// ==============================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();

// ==============================
// 🔹 MVC / Razor Pages
// ==============================
builder.Services.AddRazorPages();
builder.Logging.AddConsole();

// ==============================
// 🔹 DinkToPdf native Bibliothek laden
// ==============================
var pdfContext = new CustomAssemblyLoadContext();
pdfContext.LoadUnmanagedLibrary(Path.Combine(Directory.GetCurrentDirectory(), "Native", "libwkhtmltox.dll"));

// ==============================
// 🔹 App erstellen
// ==============================
var app = builder.Build();

// ==============================
// 🔹 Fehlerbehandlung
// ==============================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ==============================
// 🔹 Pipeline
// ==============================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Standardroute zur Startseite
app.MapGet("/", context =>
{
    context.Response.Redirect("/Willkommen");
    return Task.CompletedTask;
});

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");
app.MapHub<SISHub>("/sisHub");

// ==============================
// 🔹 Rollen & Datenbank-Seeding
// ==============================
async Task SeedRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Editor", "Viewer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// ==============================
// 🔹 Migration + Initialdaten
// ==============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        await SeedRolesAsync(services);

        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Datenbankmigration ausführen
        await dbContext.Database.MigrateAsync();

        // Seed-Initialdaten (Admin, Beispiel-Dokumente etc.)
        await DbInitializer.SeedAsync(dbContext);

        Console.WriteLine("✅ Datenbank erfolgreich migriert und initialisiert.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Fehler beim Initialisieren der Datenbank: {ex.Message}");
    }
}

// ==============================
// 🔹 App starten
// ==============================
try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("💥 Fatal error:");
    Console.WriteLine(ex);
    throw;
}
