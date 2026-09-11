using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddSemanticSearchQueryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SemanticSearchQueryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedTopK = table.Column<int>(type: "int", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    TopResultScore = table.Column<float>(type: "real", nullable: true),
                    ScopeDetected = table.Column<bool>(type: "bit", nullable: false),
                    DetectedPoetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedCategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScopeDetectionDisabled = table.Column<bool>(type: "bit", nullable: false),
                    ClickedPoemId = table.Column<int>(type: "int", nullable: true),
                    ClickedResultRank = table.Column<int>(type: "int", nullable: true),
                    ClickedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemanticSearchQueryLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SemanticSearchQueryLogs");
        }
    }
}
