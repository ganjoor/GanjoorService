using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorIndices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullUrl",
                table: "GanjoorPoems",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullUrl",
                table: "GanjoorCategories",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoems_FullUrl",
                table: "GanjoorPoems",
                column: "FullUrl");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_FullUrl",
                table: "GanjoorCategories",
                column: "FullUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoems_FullUrl",
                table: "GanjoorPoems");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorCategories_FullUrl",
                table: "GanjoorCategories");

            migrationBuilder.AlterColumn<string>(
                name: "FullUrl",
                table: "GanjoorPoems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullUrl",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
