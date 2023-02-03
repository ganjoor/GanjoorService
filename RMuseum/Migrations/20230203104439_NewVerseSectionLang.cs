using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class NewVerseSectionLang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NewVerse",
                table: "GanjoorVerseVOrderText",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NewVerseResult",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "GanjoorPoemSections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewVerse",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "NewVerseResult",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "GanjoorPoemSections");
        }
    }
}
