using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class SugCorrections4Sections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoemSectionCorrections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Rhythm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalRhythm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RhythmResult = table.Column<int>(type: "int", nullable: false),
                    Rhythm2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalRhythm2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RhythmResult2 = table.Column<int>(type: "int", nullable: false),
                    BreakFromVerse1VOrder = table.Column<int>(type: "int", nullable: true),
                    BreakFromVerse1VOrderResult = table.Column<int>(type: "int", nullable: false),
                    BreakFromVerse2VOrder = table.Column<int>(type: "int", nullable: true),
                    BreakFromVerse2VOrderResult = table.Column<int>(type: "int", nullable: false),
                    BreakFromVerse3VOrder = table.Column<int>(type: "int", nullable: true),
                    BreakFromVerse3VOrderResult = table.Column<int>(type: "int", nullable: false),
                    BreakFromVerse4VOrder = table.Column<int>(type: "int", nullable: true),
                    BreakFromVerse4VOrderResult = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicationOrder = table.Column<int>(type: "int", nullable: false),
                    AffectedThePoem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoemSectionCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemSectionCorrections_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoemSectionCorrections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemSectionCorrections_GanjoorPoemSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "GanjoorPoemSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSectionCorrections_ReviewerUserId",
                table: "GanjoorPoemSectionCorrections",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSectionCorrections_SectionId",
                table: "GanjoorPoemSectionCorrections",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemSectionCorrections_UserId",
                table: "GanjoorPoemSectionCorrections",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoemSectionCorrections");
        }
    }
}
