using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorPoemNewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RhymeLetters",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrlSlug",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerseCount",
                table: "GanjoorMetres",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RhymeLetters",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "SourceUrlSlug",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "VerseCount",
                table: "GanjoorMetres");
        }
    }
}
