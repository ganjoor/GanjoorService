using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorVerseCoupletIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoupletIndex",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerses_PoemId_CoupletIndex",
                table: "GanjoorVerses",
                columns: new[] { "PoemId", "CoupletIndex" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorVerses_PoemId_CoupletIndex",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "CoupletIndex",
                table: "GanjoorVerses");
        }
    }
}
