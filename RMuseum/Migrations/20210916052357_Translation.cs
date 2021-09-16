using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class Translation : Migration
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
                name: "GanjoorPoemTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    TText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GanjoorPoemTranslationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorVerseTranslation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorVerseTranslation_GanjoorPoemTranslations_GanjoorPoemTranslationId",
                        column: x => x.GanjoorPoemTranslationId,
                        principalTable: "GanjoorPoemTranslations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorVerseTranslation_GanjoorVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "GanjoorVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorLanguages_Name",
                table: "GanjoorLanguages",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorVerseTranslation");

            migrationBuilder.DropTable(
                name: "GanjoorPoemTranslations");

            migrationBuilder.DropTable(
                name: "GanjoorLanguages");
        }
    }
}
