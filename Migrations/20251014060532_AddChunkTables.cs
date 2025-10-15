using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DmsProjeckt.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "DokumentChunks");

            migrationBuilder.AddColumn<string>(
                name: "StorageLocation",
                table: "Dokumente",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirebasePath",
                table: "DokumentChunks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageLocation",
                table: "Dokumente");

            migrationBuilder.DropColumn(
                name: "FirebasePath",
                table: "DokumentChunks");

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "DokumentChunks",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
