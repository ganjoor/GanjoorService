using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class PoemCorLang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageReviewResult",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "LanguageReviewResult",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "GanjoorPoemCorrections");
        }
    }
}
