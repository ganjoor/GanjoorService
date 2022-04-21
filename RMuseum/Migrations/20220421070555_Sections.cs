using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class Sections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionIndex1",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionIndex2",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionIndex3",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionIndex4",
                table: "GanjoorVerses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalRhythm2",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalRhythm3",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalRhythm4",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rhythm2",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rhythm2Result",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Rhythm3",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rhythm3Result",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Rhythm4",
                table: "GanjoorPoemCorrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rhythm4Result",
                table: "GanjoorPoemCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GanjoorCachedRelatedSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    SectionIndex = table.Column<int>(type: "int", nullable: false),
                    PoetId = table.Column<int>(type: "int", nullable: false),
                    RelationOrder = table.Column<int>(type: "int", nullable: false),
                    PoetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoetImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HtmlExcerpt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetPoemId = table.Column<int>(type: "int", nullable: false),
                    TargetSectionIndex = table.Column<int>(type: "int", nullable: false),
                    PoetMorePoemsLikeThisCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCachedRelatedSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCachedRelatedSections_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    GanjoorMetreRefSectionIndex = table.Column<int>(type: "int", nullable: true),
                    RhymeLetters = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PlainText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HtmlText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoemFormat = table.Column<int>(type: "int", nullable: true)
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
                name: "IX_GanjoorCachedRelatedSections_PoemId_SectionIndex",
                table: "GanjoorCachedRelatedSections",
                columns: new[] { "PoemId", "SectionIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_GanjoorMetreId_RhymeLetters",
                table: "GanjoorPoemSections",
                columns: new[] { "GanjoorMetreId", "RhymeLetters" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_GanjoorMetreId_RhymeLetters_Id",
                table: "GanjoorPoemSections",
                columns: new[] { "GanjoorMetreId", "RhymeLetters", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_PoemId_Index",
                table: "GanjoorPoemSections",
                columns: new[] { "PoemId", "Index" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_PoetId",
                table: "GanjoorPoemSections",
                column: "PoetId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSections_RhymeLetters",
                table: "GanjoorPoemSections",
                column: "RhymeLetters");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorCachedRelatedSections");

            migrationBuilder.DropTable(
                name: "GanjoorPoemSections");

            migrationBuilder.DropColumn(
                name: "SectionIndex1",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "SectionIndex2",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "SectionIndex3",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "SectionIndex4",
                table: "GanjoorVerses");

            migrationBuilder.DropColumn(
                name: "OriginalRhythm2",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "OriginalRhythm3",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "OriginalRhythm4",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "Rhythm2",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "Rhythm2Result",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "Rhythm3",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "Rhythm3Result",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "Rhythm4",
                table: "GanjoorPoemCorrections");

            migrationBuilder.DropColumn(
                name: "Rhythm4Result",
                table: "GanjoorPoemCorrections");
        }
    }
}
