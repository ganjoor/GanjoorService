using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorUserBookmarkRemoveVerseIdDep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks");

            migrationBuilder.AlterColumn<int>(
                name: "VerseId",
                table: "GanjoorUserBookmarks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks",
                columns: new[] { "UserId", "PoemId", "VerseId" },
                unique: true,
                filter: "[VerseId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks");

            migrationBuilder.AlterColumn<int>(
                name: "VerseId",
                table: "GanjoorUserBookmarks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks",
                columns: new[] { "UserId", "PoemId", "VerseId" },
                unique: true);
        }
    }
}
