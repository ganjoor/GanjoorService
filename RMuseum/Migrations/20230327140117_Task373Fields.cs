using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class Task373Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoupletSummary",
                table: "GanjoorVerseVOrderText",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageReviewResult",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OriginalCoupletSummary",
                table: "GanjoorVerseVOrderText",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalLanguageId",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryReviewResult",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CoupletSummary",
                table: "GanjoorVerses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideMyName",
                table: "GanjoorPoemSectionCorrections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OriginalPoemFormat",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoemFormat",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoemFormatReviewResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PoemSummary",
                table: "GanjoorPoems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HideMyName",
                table: "GanjoorPoemCorrections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalPoemSummary",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoemSummary",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryReviewResult",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerses_LanguageId",
                table: "GanjoorVerses",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorVerses_GanjoorLanguages_LanguageId",
                table: "GanjoorVerses",
                column: "LanguageId",
                principalTable: "GanjoorLanguages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorVerses_GanjoorLanguages_LanguageId",
                table: "GanjoorVerses");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorVerses_LanguageId",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "CoupletSummary",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "LanguageReviewResult",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "OriginalCoupletSummary",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "OriginalLanguageId",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "SummaryReviewResult",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "CoupletSummary",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "HideMyName",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "OriginalPoemFormat",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "PoemFormat",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "PoemFormatReviewResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "PoemSummary",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "HideMyName",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "OriginalPoemSummary",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "PoemSummary",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "SummaryReviewResult",
                table: "GanjoorPoemCorrections");
        }
    }
}
