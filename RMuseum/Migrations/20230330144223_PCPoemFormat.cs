using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class PCPoemFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalPoemFormat",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoemFormat",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoemFormatReviewResult",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalPoemFormat",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "PoemFormat",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "PoemFormatReviewResult",
                table: "GanjoorPoemCorrections");
        }
    }
}
