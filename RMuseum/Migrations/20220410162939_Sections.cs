using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class Sections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ForthSectionIndex",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondSectionIndex",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionIndex",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThirdSectionIndex",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GanjoorPoemSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    PoetId = table.Column<int>(type: "int", nullable: true),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    SectionType = table.Column<int>(type: "int", nullable: false),
                    VerseType = table.Column<int>(type: "int", nullable: false),
                    GanjoorMetreId = table.Column<int>(type: "int", nullable: true),
                    RhymeLetters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlainText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HtmlText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoemSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemSections_GanjoorMetres_GanjoorMetreId",
                        column: x => x.GanjoorMetreId,
                        principalTable: "GanjoorMetres",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoemSections_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemSections_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_GanjoorMetreId",
                table: "GanjoorPoemSections",
                column: "GanjoorMetreId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_PoemId",
                table: "GanjoorPoemSections",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_PoetId",
                table: "GanjoorPoemSections",
                column: "PoetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoemSections");

            migrationBuilder.DropColumn(
                name: "ForthSectionIndex",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "SecondSectionIndex",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "SectionIndex",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "ThirdSectionIndex",
                table: "GanjoorVerses");
        }
    }
}
