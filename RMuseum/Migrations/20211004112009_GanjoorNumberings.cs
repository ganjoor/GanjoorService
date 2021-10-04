using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorNumberings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorNumberings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartCatId = table.Column<int>(type: "int", nullable: false),
                    EndCatId = table.Column<int>(type: "int", nullable: true),
                    TotalLines = table.Column<int>(type: "int", nullable: false),
                    TotalVerses = table.Column<int>(type: "int", nullable: false),
                    TotalCouplets = table.Column<int>(type: "int", nullable: false),
                    TotalParagraphs = table.Column<int>(type: "int", nullable: false),
                    LastCountingDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorNumberings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorNumberings_GanjoorCategories_EndCatId",
                        column: x => x.EndCatId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorNumberings_GanjoorCategories_StartCatId",
                        column: x => x.StartCatId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorVerseNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumberingId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    IsPoemVerse = table.Column<bool>(type: "bit", nullable: false),
                    SameTypeNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorVerseNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorVerseNumbers_GanjoorNumberings_NumberingId",
                        column: x => x.NumberingId,
                        principalTable: "GanjoorNumberings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorNumberings_EndCatId",
                table: "GanjoorNumberings",
                column: "EndCatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorNumberings_StartCatId",
                table: "GanjoorNumberings",
                column: "StartCatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerseNumbers_NumberingId_PoemId_CoupletIndex",
                table: "GanjoorVerseNumbers",
                columns: new[] { "NumberingId", "PoemId", "CoupletIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerseNumbers_PoemId_CoupletIndex",
                table: "GanjoorVerseNumbers",
                columns: new[] { "PoemId", "CoupletIndex" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorVerseNumbers");

            migrationBuilder.DropTable(
                name: "GanjoorNumberings");
        }
    }
}
