using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorVerseNoVerseObjInGUBookmark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorUserBookmarks_GanjoorVerses_Verse2Id",
                table: "GanjoorUserBookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorUserBookmarks_GanjoorVerses_VerseId",
                table: "GanjoorUserBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserBookmarks_Verse2Id",
                table: "GanjoorUserBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserBookmarks_VerseId",
                table: "GanjoorUserBookmarks");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_Verse2Id",
                table: "GanjoorUserBookmarks",
                column: "Verse2Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_VerseId",
                table: "GanjoorUserBookmarks",
                column: "VerseId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorUserBookmarks_GanjoorVerses_Verse2Id",
                table: "GanjoorUserBookmarks",
                column: "Verse2Id",
                principalTable: "GanjoorVerses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorUserBookmarks_GanjoorVerses_VerseId",
                table: "GanjoorUserBookmarks",
                column: "VerseId",
                principalTable: "GanjoorVerses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
