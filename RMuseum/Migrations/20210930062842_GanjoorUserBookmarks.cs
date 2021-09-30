using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorUserBookmarks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorUserBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    VerseId = table.Column<int>(type: "int", nullable: false),
                    Verse2Id = table.Column<int>(type: "int", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorUserBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorUserBookmarks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorUserBookmarks_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorUserBookmarks_GanjoorVerses_Verse2Id",
                        column: x => x.Verse2Id,
                        principalTable: "GanjoorVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorUserBookmarks_GanjoorVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "GanjoorVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_PoemId",
                table: "GanjoorUserBookmarks",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_UserId_PoemId_VerseId",
                table: "GanjoorUserBookmarks",
                columns: new[] { "UserId", "PoemId", "VerseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_Verse2Id",
                table: "GanjoorUserBookmarks",
                column: "Verse2Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserBookmarks_VerseId",
                table: "GanjoorUserBookmarks",
                column: "VerseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorUserBookmarks");
        }
    }
}
