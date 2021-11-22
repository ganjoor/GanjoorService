using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class GanjoorCachedRelatedPoem_FullUrlIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullUrl",
                table: "GanjoorCachedRelatedPoems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCachedRelatedPoems_FullUrl",
                table: "GanjoorCachedRelatedPoems",
                column: "FullUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorCachedRelatedPoems_FullUrl",
                table: "GanjoorCachedRelatedPoems");

            migrationBuilder.AlterColumn<string>(
                name: "FullUrl",
                table: "GanjoorCachedRelatedPoems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
