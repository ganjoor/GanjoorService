using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class Translations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NativeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RightToLeft = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorLanguages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoemTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoemTranslations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoemTranslations_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoemTranslations_PoemTranslations_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "PoemTranslations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GanjoorVerseTranslation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerseId = table.Column<int>(type: "int", nullable: false),
                    TText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GanjoorPoemTranslationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorVerseTranslation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorVerseTranslation_GanjoorVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "GanjoorVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorVerseTranslation_PoemTranslations_GanjoorPoemTranslationId",
                        column: x => x.GanjoorPoemTranslationId,
                        principalTable: "PoemTranslations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorLanguages_Name",
                table: "GanjoorLanguages",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerseTranslation_GanjoorPoemTranslationId",
                table: "GanjoorVerseTranslation",
                column: "GanjoorPoemTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerseTranslation_VerseId",
                table: "GanjoorVerseTranslation",
                column: "VerseId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemTranslations_LanguageId",
                table: "PoemTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemTranslations_PoemId",
                table: "PoemTranslations",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemTranslations_UserId",
                table: "PoemTranslations",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorLanguages");

            migrationBuilder.DropTable(
                name: "GanjoorVerseTranslation");

            migrationBuilder.DropTable(
                name: "PoemTranslations");
        }
    }
}
