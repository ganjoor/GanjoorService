using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class PDFLibNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileMD5CheckSum",
                table: "PDFBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "PDFBooks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageFolderName",
                table: "PDFBooks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileMD5CheckSum",
                table: "PDFBooks");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "PDFBooks");

            migrationBuilder.DropColumn(
                name: "StorageFolderName",
                table: "PDFBooks");
        }
    }
}
