using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class CommentsRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorComments_PoemId",
                table: "GanjoorComments");

            migrationBuilder.AddColumn<int>(
                name: "DislikeCount",
                table: "GanjoorComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "GanjoorComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortKey",
                table: "GanjoorComments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GanjoorCommentReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GanjoorCommentId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<short>(type: "smallint", nullable: false),
                    ReactionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCommentReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCommentReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorCommentReactions_GanjoorComments_GanjoorCommentId",
                        column: x => x.GanjoorCommentId,
                        principalTable: "GanjoorComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_PoemId_CommentDate",
                table: "GanjoorComments",
                columns: new[] { "PoemId", "CommentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_PoemId_SortKey",
                table: "GanjoorComments",
                columns: new[] { "PoemId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCommentReactions_GanjoorCommentId_UserId",
                table: "GanjoorCommentReactions",
                columns: new[] { "GanjoorCommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCommentReactions_PoemId_UserId",
                table: "GanjoorCommentReactions",
                columns: new[] { "PoemId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCommentReactions_UserId",
                table: "GanjoorCommentReactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorCommentReactions");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorComments_PoemId_CommentDate",
                table: "GanjoorComments");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorComments_PoemId_SortKey",
                table: "GanjoorComments");

            migrationBuilder.DropColumn(
                name: "DislikeCount",
                table: "GanjoorComments");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "GanjoorComments");

            migrationBuilder.DropColumn(
                name: "SortKey",
                table: "GanjoorComments");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_PoemId",
                table: "GanjoorComments",
                column: "PoemId");
        }
    }
}
