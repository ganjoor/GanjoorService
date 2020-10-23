using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorCachingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullUrl",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullUrl",
                table: "GanjoorCategories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullUrl",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "FullUrl",
                table: "GanjoorCategories");
        }
    }
}
