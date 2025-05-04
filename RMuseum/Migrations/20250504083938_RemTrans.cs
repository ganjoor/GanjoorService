using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class RemTrans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorVerseTranslation");

            migrationBuilder.DropTable(
                name: "GanjoorPoemTranslations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoemTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemTranslations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemTranslations_GanjoorLanguages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "GanjoorLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemTranslations_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorVerseTranslation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerseId = table.Column<int>(type: "int", nullable: false),
                    GanjoorPoemTranslationId = table.Column<int>(type: "int", nullable: true),
                    TText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorVerseTranslation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorVerseTranslation_GanjoorPoemTranslations_GanjoorPoemTranslationId",
                        column: x => x.GanjoorPoemTranslationId,
                        principalTable: "GanjoorPoemTranslations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorVerseTranslation_GanjoorVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "GanjoorVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemTranslations_LanguageId",
                table: "GanjoorPoemTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemTranslations_PoemId",
                table: "GanjoorPoemTranslations",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemTranslations_UserId",
                table: "GanjoorPoemTranslations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerseTranslation_GanjoorPoemTranslationId",
                table: "GanjoorVerseTranslation",
                column: "GanjoorPoemTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerseTranslation_VerseId",
                table: "GanjoorVerseTranslation",
                column: "VerseId");
        }
    }
}
