using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class PinterestLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PinterestLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GanjoorPostId = table.Column<int>(nullable: false),
                    GanjoorUrl = table.Column<string>(nullable: true),
                    GanjoorTitle = table.Column<string>(nullable: true),
                    AltText = table.Column<string>(nullable: true),
                    LinkType = table.Column<int>(nullable: false),
                    PinterestUrl = table.Column<string>(nullable: true),
                    PinterestImageUrl = table.Column<string>(nullable: true),
                    SuggestedById = table.Column<Guid>(nullable: true),
                    SuggestionDate = table.Column<DateTime>(nullable: false),
                    ReviewerId = table.Column<Guid>(nullable: true),
                    ReviewDate = table.Column<DateTime>(nullable: false),
                    ReviewResult = table.Column<int>(nullable: false),
                    ReviewDesc = table.Column<string>(nullable: true),
                    ArtifactId = table.Column<Guid>(nullable: true),
                    ItemId = table.Column<Guid>(nullable: true),
                    Synchronized = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinterestLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PinterestLinks_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PinterestLinks_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PinterestLinks_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PinterestLinks_AspNetUsers_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PinterestLinks_ArtifactId",
                table: "PinterestLinks",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_PinterestLinks_ItemId",
                table: "PinterestLinks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PinterestLinks_ReviewerId",
                table: "PinterestLinks",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PinterestLinks_SuggestedById",
                table: "PinterestLinks",
                column: "SuggestedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PinterestLinks");
        }
    }
}
