using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class QP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorQuotedPoems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    RelatedPoemId = table.Column<int>(type: "int", nullable: true),
                    IsPriorToRelated = table.Column<bool>(type: "bit", nullable: false),
                    ChosenForMainList = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CachedRelatedPoemPoetDeathYearInLHijri = table.Column<int>(type: "int", nullable: false),
                    CachedRelatedPoemPoetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CachedRelatedPoemPoetUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CachedRelatedPoemPoetImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CachedRelatedPoemFullTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CachedRelatedPoemFullUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoupletVerse1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoupletVerse1ShouldBeEmphasized = table.Column<bool>(type: "bit", nullable: false),
                    CoupletVerse2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoupletVerse2ShouldBeEmphasized = table.Column<bool>(type: "bit", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: true),
                    RelatedCoupletVerse1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedCoupletVerse1ShouldBeEmphasized = table.Column<bool>(type: "bit", nullable: false),
                    RelatedCoupletVerse2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedCoupletVerse2ShouldBeEmphasized = table.Column<bool>(type: "bit", nullable: false),
                    RelatedCoupletIndex = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorQuotedPoems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorQuotedPoems");
        }
    }
}
