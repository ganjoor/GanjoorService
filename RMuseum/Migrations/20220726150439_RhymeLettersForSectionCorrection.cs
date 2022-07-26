using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class RhymeLettersForSectionCorrection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalRhymeLetters",
                table: "GanjoorPoemSectionCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RhymeLetters",
                table: "GanjoorPoemSectionCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RhymeLettersReviewResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OriginalRhymeLetters",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RhymeLetters",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RhymeLettersReviewResult",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalRhymeLetters",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "RhymeLetters",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "RhymeLettersReviewResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "OriginalRhymeLetters",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "RhymeLetters",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "RhymeLettersReviewResult",
                table: "GanjoorPoemCorrections");
        }
    }
}
