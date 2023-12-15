using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class GanjoorQuotedPoemPoet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PoetId",
                table: "GanjoorQuotedPoems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelatedPoetId",
                table: "GanjoorQuotedPoems",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoetId",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "RelatedPoetId",
                table: "GanjoorQuotedPoems");
        }
    }
}
