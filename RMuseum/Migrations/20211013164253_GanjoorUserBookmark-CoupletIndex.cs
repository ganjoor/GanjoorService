using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorUserBookmarkCoupletIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_CoupletIndex",
                table: "GanjoorUserBookmarks",
                columns: new[] { "UserId", "PoemId", "CoupletIndex" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_CoupletIndex",
                table: "GanjoorUserBookmarks");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks",
                columns: new[] { "UserId", "PoemId", "VerseId" },
                unique: true,
                filter: "[VerseId] IS NOT NULL");
        }
    }
}
