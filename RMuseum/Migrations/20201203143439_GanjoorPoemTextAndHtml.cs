using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorPoemTextAndHtml : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HtmlText",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlainText",
                table: "GanjoorPoems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HtmlText",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "PlainText",
                table: "GanjoorPoems");
        }
    }
}
