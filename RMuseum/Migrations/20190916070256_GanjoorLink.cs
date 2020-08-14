using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GanjoorPostId = table.Column<int>(nullable: false),
                    GanjoorUrl = table.Column<string>(nullable: true),
                    GanjoorTitle = table.Column<string>(nullable: true),
                    ArtifactId = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: true),
                    SuggestedById = table.Column<Guid>(nullable: false),
                    SuggestionDate = table.Column<DateTime>(nullable: false),
                    ReviewerId = table.Column<Guid>(nullable: true),
                    ReviewDate = table.Column<DateTime>(nullable: false),
                    ReviewResult = table.Column<int>(nullable: false),
                    Synchronized = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorLinks_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorLinks_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorLinks_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorLinks_AspNetUsers_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorLinks_ArtifactId",
                table: "GanjoorLinks",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorLinks_ItemId",
                table: "GanjoorLinks",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorLinks_ReviewerId",
                table: "GanjoorLinks",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorLinks_SuggestedById",
                table: "GanjoorLinks",
                column: "SuggestedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorLinks");
        }
    }
}
