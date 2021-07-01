using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorLinkOriginalSourceUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LinkToOriginalSource",
                table: "GanjoorLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalSourceUrl",
                table: "GanjoorLinks",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkToOriginalSource",
                table: "GanjoorLinks");

            migrationBuilder.DropColumn(
                name: "OriginalSourceUrl",
                table: "GanjoorLinks");
        }
    }
}
