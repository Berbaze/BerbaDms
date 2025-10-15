using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmsProjeckt.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkStorageSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChunked",
                table: "Dokumente",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DokumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DokumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DokumentChunks_Dokumente_DokumentId",
                        column: x => x.DokumentId,
                        principalTable: "Dokumente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DokumentVersionChunks",
                columns: table => new
                {
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    ChunkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentVersionChunks", x => new { x.VersionId, x.ChunkId });
                    table.ForeignKey(
                        name: "FK_DokumentVersionChunks_DokumentChunks_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "DokumentChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DokumentVersionChunks_DokumentVersionen_VersionId",
                        column: x => x.VersionId,
                        principalTable: "DokumentVersionen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DokumentChunks_DokumentId",
                table: "DokumentChunks",
                column: "DokumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentVersionChunks_ChunkId",
                table: "DokumentVersionChunks",
                column: "ChunkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DokumentVersionChunks");

            migrationBuilder.DropTable(
                name: "DokumentChunks");

            migrationBuilder.DropColumn(
                name: "IsChunked",
                table: "Dokumente");
        }
    }
}
