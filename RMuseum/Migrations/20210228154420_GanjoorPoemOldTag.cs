using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorPoemOldTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OldTag",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldTagPageUrl",
                table: "GanjoorPoems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldTag",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "OldTagPageUrl",
                table: "GanjoorPoems");
        }
    }
}
