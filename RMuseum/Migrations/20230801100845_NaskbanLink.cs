using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class NaskbanLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTextOriginalSource",
                table: "PinterestLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "NaskbanLinkId",
                table: "PinterestLinks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PDFBookId",
                table: "PinterestLinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "PinterestLinks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PDFGanjoorLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GanjoorPostId = table.Column<int>(type: "int", nullable: false),
                    GanjoorUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GanjoorTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PDFBookId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    SuggestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewResult = table.Column<int>(type: "int", nullable: false),
                    Synchronized = table.Column<bool>(type: "bit", nullable: false),
                    IsTextOriginalSource = table.Column<bool>(type: "bit", nullable: false),
                    PDFPageTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalThumbnailImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFGanjoorLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PDFGanjoorLinks_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PDFGanjoorLinks_AspNetUsers_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PDFGanjoorLinks_ReviewerId",
                table: "PDFGanjoorLinks",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFGanjoorLinks_SuggestedById",
                table: "PDFGanjoorLinks",
                column: "SuggestedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PDFGanjoorLinks");

            migrationBuilder.DropColumn(
                name: "IsTextOriginalSource",
                table: "PinterestLinks");

            migrationBuilder.DropColumn(
                name: "NaskbanLinkId",
                table: "PinterestLinks");

            migrationBuilder.DropColumn(
                name: "PDFBookId",
                table: "PinterestLinks");

            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "PinterestLinks");
        }
    }
}
