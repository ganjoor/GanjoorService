using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class GanjoorPoemSectionIdIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_GanjoorMetreId_RhymeLetters_Id",
                table: "GanjoorPoemSections",
                columns: new[] { "GanjoorMetreId", "RhymeLetters", "Id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoemSections_GanjoorMetreId_RhymeLetters_Id",
                table: "GanjoorPoemSections");
        }
    }
}
