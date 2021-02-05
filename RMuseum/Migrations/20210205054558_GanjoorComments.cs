using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorComments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    AuthorName = table.Column<string>(nullable: true),
                    AuthorEmail = table.Column<string>(nullable: true),
                    AuthorUrl = table.Column<string>(nullable: true),
                    AuthorIpAddress = table.Column<string>(nullable: true),
                    CommentDate = table.Column<DateTime>(nullable: false),
                    HtmlComment = table.Column<string>(nullable: true),
                    InReplyToId = table.Column<int>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorComments_GanjoorComments_InReplyToId",
                        column: x => x.InReplyToId,
                        principalTable: "GanjoorComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorComments_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_InReplyToId",
                table: "GanjoorComments",
                column: "InReplyToId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_PoemId",
                table: "GanjoorComments",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_UserId",
                table: "GanjoorComments",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorComments");
        }
    }
}
