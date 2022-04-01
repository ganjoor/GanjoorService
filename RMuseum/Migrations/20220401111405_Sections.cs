using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class Sections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PoemSectionIndex",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondPoemSectionIndex",
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
                    Index = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    SectionType = table.Column<int>(type: "int", nullable: false),
                    GanjoorMetreId = table.Column<int>(type: "int", nullable: true),
                    RhymeLetters = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_GanjoorMetreId",
                table: "GanjoorPoemSections",
                column: "GanjoorMetreId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_PoemId",
                table: "GanjoorPoemSections",
                column: "PoemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoemSections");

            migrationBuilder.DropColumn(
                name: "PoemSectionIndex",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "SecondPoemSectionIndex",
                table: "GanjoorVerses");
        }
    }
}
