using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmsProjeckt.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Abteilungen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abteilungen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogAdmins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CssClass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Beschreibung = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DokumentIndex",
                columns: table => new
                {
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dateiname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Beschreibung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OCRText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kategorie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErkannteKategorie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rechnungsnummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kundennummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rechnungsbetrag = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Nettobetrag = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Gesamtbetrag = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Steuerbetrag = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Rechnungsdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Lieferdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Faelligkeitsdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Zahlungsbedingungen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lieferart = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArtikelAnzahl = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IBAN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bankverbindung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SteuerNr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UIDNummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbsenderAdresse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnsprechPartner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zeitraum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kundenname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmenName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadaten = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Autor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Betreff = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Schluesselwoerter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjectPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentIndex", x => x.DokumentId);
                });

            migrationBuilder.CreateTable(
                name: "DokumentSignatur",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Width = table.Column<float>(type: "real", nullable: false),
                    Height = table.Column<float>(type: "real", nullable: false),
                    ImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentSignatur", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignatureRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Vorname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nachname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Geburtsdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByAdminId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmenName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminId = table.Column<int>(type: "int", nullable: true),
                    ProfilbildUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AbteilungId = table.Column<int>(type: "int", nullable: true),
                    SignaturePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Abteilungen_AbteilungId",
                        column: x => x.AbteilungId,
                        principalTable: "Abteilungen",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageRead",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageRead", x => new { x.MessageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_MessageRead_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotificationTypeId = table.Column<int>(type: "int", nullable: false),
                    ActionLink = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_NotificationTypes_NotificationTypeId",
                        column: x => x.NotificationTypeId,
                        principalTable: "NotificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatGroupMembers",
                columns: table => new
                {
                    ChatGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatGroupMembers", x => new { x.ChatGroupId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChatGroupMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatGroupMembers_ChatGroups_ChatGroupId",
                        column: x => x.ChatGroupId,
                        principalTable: "ChatGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DokumentRechte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentRechte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DokumentRechte_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FolderPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FolderPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrantedByAdminId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderPermissions_AspNetUsers_GrantedByAdminId",
                        column: x => x.GrantedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FolderPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kommentare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BenutzerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kommentare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kommentare_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Kunden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vorname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirmenName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kunden", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kunden_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notiz",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Titel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Inhalt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LetzteBearbeitung = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notiz", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notiz_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserDashboardItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DashboardItemId = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    Favorit = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDashboardItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDashboardItem_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDashboardItem_DashboardItem_DashboardItemId",
                        column: x => x.DashboardItemId,
                        principalTable: "DashboardItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NotificationTypeId = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AdvanceMinutes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettings_NotificationTypes_NotificationTypeId",
                        column: x => x.NotificationTypeId,
                        principalTable: "NotificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflows_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SendAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KundeBenutzer",
                columns: table => new
                {
                    KundenId = table.Column<int>(type: "int", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KundeBenutzer", x => new { x.KundenId, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_KundeBenutzer_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KundeBenutzer_Kunden_KundenId",
                        column: x => x.KundenId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoritNote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotizId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HinzugefuegtAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoritNote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavoritNote_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoritNote_Notiz_NotizId",
                        column: x => x.NotizId,
                        principalTable: "Notiz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSharedNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotizId = table.Column<int>(type: "int", nullable: false),
                    SharedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SharedToUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSharedNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSharedNotes_AspNetUsers_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSharedNotes_AspNetUsers_SharedToUserId",
                        column: x => x.SharedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSharedNotes_Notiz_NotizId",
                        column: x => x.NotizId,
                        principalTable: "Notiz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Steps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    TaskCreated = table.Column<bool>(type: "bit", nullable: false),
                    Kategorie = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Steps_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Steps_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aufgaben",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Titel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Beschreibung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaelligBis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Prioritaet = table.Column<int>(type: "int", nullable: false),
                    Erledigt = table.Column<bool>(type: "bit", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VonUser = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FuerUser = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StepId = table.Column<int>(type: "int", nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
                    WorkflowId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aufgaben", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aufgaben_AspNetUsers_FuerUser",
                        column: x => x.FuerUser,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_AspNetUsers_VonUser",
                        column: x => x.VonUser,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Steps_StepId",
                        column: x => x.StepId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Aufgaben_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StepKommentare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepKommentare", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepKommentare_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StepKommentare_Steps_StepId",
                        column: x => x.StepId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dokumente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dateipfad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Beschreibung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErkannteKategorie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KundeId = table.Column<int>(type: "int", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DokumentStatus = table.Column<int>(type: "int", nullable: false),
                    dtStatus = table.Column<int>(type: "int", nullable: false),
                    ObjectPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AufgabeId = table.Column<int>(type: "int", nullable: true),
                    EstSigne = table.Column<bool>(type: "bit", nullable: true),
                    WorkflowId = table.Column<int>(type: "int", nullable: true),
                    IsIndexed = table.Column<bool>(type: "bit", nullable: true),
                    IstFavorit = table.Column<bool>(type: "bit", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsUpdated = table.Column<bool>(type: "bit", nullable: false),
                    StepId = table.Column<int>(type: "int", nullable: true),
                    OriginalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsVersion = table.Column<bool>(type: "bit", nullable: false),
                    AbteilungId = table.Column<int>(type: "int", nullable: true),
                    Titel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dateiname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HochgeladenAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kategorie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadatenId = table.Column<int>(type: "int", nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LetzteAenderung = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dokumente_Abteilungen_AbteilungId",
                        column: x => x.AbteilungId,
                        principalTable: "Abteilungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Dokumente_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dokumente_Aufgaben_AufgabeId",
                        column: x => x.AufgabeId,
                        principalTable: "Aufgaben",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dokumente_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Archive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchivName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivPfad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ArchivDatum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BenutzerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grund = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadatenJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IstAktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Archive_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogDokumente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenutzerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aktion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Zeitstempel = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogDokumente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogDokumente_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Aktion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BenutzerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Zeitstempel = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_BenutzerId",
                        column: x => x.BenutzerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BenutzerMetadaten",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErzeugtAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenutzerMetadaten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenutzerMetadaten_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DokumenteStep",
                columns: table => new
                {
                    DokumenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumenteStep", x => new { x.DokumenteId, x.StepsId });
                    table.ForeignKey(
                        name: "FK_DokumenteStep_Dokumente_DokumenteId",
                        column: x => x.DokumenteId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DokumenteStep_Steps_StepsId",
                        column: x => x.StepsId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DokumenteWorkflow",
                columns: table => new
                {
                    DokumenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumenteWorkflow", x => new { x.DokumenteId, x.WorkflowsId });
                    table.ForeignKey(
                        name: "FK_DokumenteWorkflow_Dokumente_DokumenteId",
                        column: x => x.DokumenteId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DokumenteWorkflow_Workflows_WorkflowsId",
                        column: x => x.WorkflowsId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DokumentTags",
                columns: table => new
                {
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentTags", x => new { x.DokumentId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DokumentTags_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DokumentTags_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DokumentTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DokumentVersionen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dateiname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dateipfad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HochgeladenAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionsLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    EstSigne = table.Column<bool>(type: "bit", nullable: false),
                    IsVersion = table.Column<bool>(type: "bit", nullable: false),
                    AbteilungId = table.Column<int>(type: "int", nullable: true),
                    OriginalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentVersionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DokumentVersionen_Abteilungen_AbteilungId",
                        column: x => x.AbteilungId,
                        principalTable: "Abteilungen",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DokumentVersionen_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DokumentVersionen_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DuplicateUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuplicateUploads_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuplicateUploads_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metadaten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Titel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Beschreibung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kategorie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stichworte = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rechnungsnummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kundennummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rechnungsbetrag = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Nettobetrag = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Steuerbetrag = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Gesamtpreis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Rechnungsdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Lieferdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Faelligkeitsdatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Zahlungsbedingungen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lieferart = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArtikelAnzahl = table.Column<int>(type: "int", nullable: true),
                    SteuerNr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UIDNummer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IBAN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bankverbindung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbsenderAdresse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnsprechPartner = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zeitraum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfAutor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfBetreff = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfSchluesselwoerter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OCRText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metadaten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Metadaten_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecentHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecentHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecentHistory_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchTerm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchHistory_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserFavoritDokumente",
                columns: table => new
                {
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AngelegtAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoritDokumente", x => new { x.ApplicationUserId, x.DokumentId });
                    table.ForeignKey(
                        name: "FK_UserFavoritDokumente_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavoritDokumente_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSharedDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SharedToUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSharedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSharedDocuments_AspNetUsers_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSharedDocuments_AspNetUsers_SharedToUserId",
                        column: x => x.SharedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSharedDocuments_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Archive_DokumentId",
                table: "Archive",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AbteilungId",
                table: "AspNetUsers",
                column: "AbteilungId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogDokumente_DokumentId",
                table: "AuditLogDokumente",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_BenutzerId",
                table: "AuditLogs",
                column: "BenutzerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DokumentId",
                table: "AuditLogs",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_FuerUser",
                table: "Aufgaben",
                column: "FuerUser");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_StepId",
                table: "Aufgaben",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_VonUser",
                table: "Aufgaben",
                column: "VonUser");

            migrationBuilder.CreateIndex(
                name: "IX_Aufgaben_WorkflowId",
                table: "Aufgaben",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_BenutzerMetadaten_DokumentId",
                table: "BenutzerMetadaten",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatGroupMembers_UserId",
                table: "ChatGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_AbteilungId",
                table: "Dokumente",
                column: "AbteilungId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_ApplicationUserId",
                table: "Dokumente",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_AufgabeId",
                table: "Dokumente",
                column: "AufgabeId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumente_KundeId",
                table: "Dokumente",
                column: "KundeId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumenteStep_StepsId",
                table: "DokumenteStep",
                column: "StepsId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumenteWorkflow_WorkflowsId",
                table: "DokumenteWorkflow",
                column: "WorkflowsId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentRechte_ApplicationUserId",
                table: "DokumentRechte",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentTags_ApplicationUserId",
                table: "DokumentTags",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentTags_TagId",
                table: "DokumentTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentVersionen_AbteilungId",
                table: "DokumentVersionen",
                column: "AbteilungId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentVersionen_ApplicationUserId",
                table: "DokumentVersionen",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentVersionen_DokumentId",
                table: "DokumentVersionen",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateUploads_DokumentId",
                table: "DuplicateUploads",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateUploads_UserId",
                table: "DuplicateUploads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderPermissions_GrantedByAdminId",
                table: "FolderPermissions",
                column: "GrantedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderPermissions_UserId",
                table: "FolderPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kommentare_ApplicationUserId",
                table: "Kommentare",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_KundeBenutzer_ApplicationUserId",
                table: "KundeBenutzer",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kunden_ApplicationUserId",
                table: "Kunden",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Metadaten_DokumentId",
                table: "Metadaten",
                column: "DokumentId",
                unique: true,
                filter: "[DokumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationTypeId",
                table: "Notifications",
                column: "NotificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notiz_UserId",
                table: "Notiz",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentHistory_DokumentId",
                table: "RecentHistory",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecentHistory_UserId",
                table: "RecentHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_DokumentId",
                table: "SearchHistory",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StepKommentare_StepId",
                table: "StepKommentare",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_StepKommentare_UserId",
                table: "StepKommentare",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Steps_UserId",
                table: "Steps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Steps_WorkflowId",
                table: "Steps",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ApplicationUserId",
                table: "Tags",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDashboardItem_DashboardItemId",
                table: "UserDashboardItem",
                column: "DashboardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDashboardItem_UserId",
                table: "UserDashboardItem",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoritDokumente_DokumentId",
                table: "UserFavoritDokumente",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoritNote_NotizId",
                table: "UserFavoritNote",
                column: "NotizId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoritNote_UserId",
                table: "UserFavoritNote",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_NotificationId",
                table: "UserNotifications",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettings_NotificationTypeId",
                table: "UserNotificationSettings",
                column: "NotificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettings_UserId",
                table: "UserNotificationSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSharedDocuments_DokumentId",
                table: "UserSharedDocuments",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSharedDocuments_SharedByUserId",
                table: "UserSharedDocuments",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSharedDocuments_SharedToUserId",
                table: "UserSharedDocuments",
                column: "SharedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSharedNotes_NotizId",
                table: "UserSharedNotes",
                column: "NotizId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSharedNotes_SharedByUserId",
                table: "UserSharedNotes",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSharedNotes_SharedToUserId",
                table: "UserSharedNotes",
                column: "SharedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_UserId",
                table: "Workflows",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Archive");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogAdmins");

            migrationBuilder.DropTable(
                name: "AuditLogDokumente");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BenutzerMetadaten");

            migrationBuilder.DropTable(
                name: "ChatGroupMembers");

            migrationBuilder.DropTable(
                name: "DokumenteStep");

            migrationBuilder.DropTable(
                name: "DokumenteWorkflow");

            migrationBuilder.DropTable(
                name: "DokumentIndex");

            migrationBuilder.DropTable(
                name: "DokumentRechte");

            migrationBuilder.DropTable(
                name: "DokumentSignatur");

            migrationBuilder.DropTable(
                name: "DokumentTags");

            migrationBuilder.DropTable(
                name: "DokumentVersionen");

            migrationBuilder.DropTable(
                name: "DuplicateUploads");

            migrationBuilder.DropTable(
                name: "FolderPermissions");

            migrationBuilder.DropTable(
                name: "Kommentare");

            migrationBuilder.DropTable(
                name: "KundeBenutzer");

            migrationBuilder.DropTable(
                name: "MessageRead");

            migrationBuilder.DropTable(
                name: "Metadaten");

            migrationBuilder.DropTable(
                name: "RecentHistory");

            migrationBuilder.DropTable(
                name: "SearchHistory");

            migrationBuilder.DropTable(
                name: "SignatureRequests");

            migrationBuilder.DropTable(
                name: "StepKommentare");

            migrationBuilder.DropTable(
                name: "UserDashboardItem");

            migrationBuilder.DropTable(
                name: "UserFavoritDokumente");

            migrationBuilder.DropTable(
                name: "UserFavoritNote");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "UserNotificationSettings");

            migrationBuilder.DropTable(
                name: "UserSharedDocuments");

            migrationBuilder.DropTable(
                name: "UserSharedNotes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ChatGroups");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "DashboardItem");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Dokumente");

            migrationBuilder.DropTable(
                name: "Notiz");

            migrationBuilder.DropTable(
                name: "NotificationTypes");

            migrationBuilder.DropTable(
                name: "Aufgaben");

            migrationBuilder.DropTable(
                name: "Kunden");

            migrationBuilder.DropTable(
                name: "Steps");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Abteilungen");
        }
    }
}
