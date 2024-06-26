using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class WCSums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoetCat",
                table: "CategoryWordCounts");

            migrationBuilder.CreateTable(
                name: "CategoryWordCountSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatId = table.Column<int>(type: "int", nullable: false),
                    UniqueWordCount = table.Column<int>(type: "int", nullable: false),
                    TotalWordCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryWordCountSummaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryWordCountSummaries_CatId",
                table: "CategoryWordCountSummaries",
                column: "CatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryWordCountSummaries");

            migrationBuilder.AddColumn<bool>(
                name: "PoetCat",
                table: "CategoryWordCounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
