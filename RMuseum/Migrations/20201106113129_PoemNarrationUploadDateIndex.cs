using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class PoemNarrationUploadDateIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_UploadDate",
                table: "AudioFiles",
                column: "UploadDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_UploadDate",
                table: "AudioFiles");
        }
    }
}
