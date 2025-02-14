using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class CatSugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorCatCorrections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CatId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalDescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicationOrder = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    AffectedTheCat = table.Column<bool>(type: "bit", nullable: false),
                    HideMyName = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCatCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCatCorrections_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorCatCorrections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorCatCorrections_GanjoorCategories_CatId",
                        column: x => x.CatId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCatCorrections_CatId",
                table: "GanjoorCatCorrections",
                column: "CatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCatCorrections_ReviewerUserId",
                table: "GanjoorCatCorrections",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCatCorrections_UserId",
                table: "GanjoorCatCorrections",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorCatCorrections");
        }
    }
}
