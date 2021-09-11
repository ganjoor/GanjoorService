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
                    RightToLeft = table.Column<bool>(type: "bit", nullable: false)
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
                    Published = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemTranslations", x => x.Id);
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
                name: "VerseTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerseId = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    TText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerseTranslations_GanjoorLanguages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "GanjoorLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VerseTranslations_GanjoorVerses_VerseId",
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
                name: "IX_PoemTranslations_LanguageId_PoemId",
                table: "PoemTranslations",
                columns: new[] { "LanguageId", "PoemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoemTranslations_PoemId",
                table: "PoemTranslations",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTranslations_LanguageId_VerseId",
                table: "VerseTranslations",
                columns: new[] { "LanguageId", "VerseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerseTranslations_VerseId",
                table: "VerseTranslations",
                column: "VerseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoemTranslations");

            migrationBuilder.DropTable(
                name: "VerseTranslations");

            migrationBuilder.DropTable(
                name: "GanjoorLanguages");
        }
    }
}
