using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class CorrectionVPosDel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MarkForDelete",
                table: "GanjoorVerseVOrderText",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MarkForDeleteResult",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginalVersePosition",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersePosition",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersePositionResult",
                table: "GanjoorVerseVOrderText",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarkForDelete",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "MarkForDeleteResult",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "OriginalVersePosition",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "VersePosition",
                table: "GanjoorVerseVOrderText");

            migrationBuilder.DropColumn(
                name: "VersePositionResult",
                table: "GanjoorVerseVOrderText");
        }
    }
}
