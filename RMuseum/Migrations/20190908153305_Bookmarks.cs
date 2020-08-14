using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class Bookmarks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RAppUserId = table.Column<Guid>(nullable: false),
                    RArtifactMasterRecordId = table.Column<Guid>(nullable: true),
                    RArtifactItemRecordId = table.Column<Guid>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false),
                    RBookmarkType = table.Column<int>(nullable: false),
                    Note = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBookmarks_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBookmarks_Items_RArtifactItemRecordId",
                        column: x => x.RArtifactItemRecordId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBookmarks_Artifacts_RArtifactMasterRecordId",
                        column: x => x.RArtifactMasterRecordId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBookmarks_RAppUserId",
                table: "UserBookmarks",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookmarks_RArtifactItemRecordId",
                table: "UserBookmarks",
                column: "RArtifactItemRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookmarks_RArtifactMasterRecordId",
                table: "UserBookmarks",
                column: "RArtifactMasterRecordId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBookmarks");
        }
    }
}
