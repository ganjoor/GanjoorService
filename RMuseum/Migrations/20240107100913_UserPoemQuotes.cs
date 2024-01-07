using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class UserPoemQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Rejected",
                table: "GanjoorQuotedPoems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDate",
                table: "GanjoorQuotedPoems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "GanjoorQuotedPoems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerUserId",
                table: "GanjoorQuotedPoems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedById",
                table: "GanjoorQuotedPoems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestionDate",
                table: "GanjoorQuotedPoems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorQuotedPoems_ReviewerUserId",
                table: "GanjoorQuotedPoems",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorQuotedPoems_SuggestedById",
                table: "GanjoorQuotedPoems",
                column: "SuggestedById");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorQuotedPoems_AspNetUsers_ReviewerUserId",
                table: "GanjoorQuotedPoems",
                column: "ReviewerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorQuotedPoems_AspNetUsers_SuggestedById",
                table: "GanjoorQuotedPoems",
                column: "SuggestedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorQuotedPoems_AspNetUsers_ReviewerUserId",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorQuotedPoems_AspNetUsers_SuggestedById",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorQuotedPoems_ReviewerUserId",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorQuotedPoems_SuggestedById",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "Rejected",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "ReviewDate",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "SuggestedById",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropColumn(
                name: "SuggestionDate",
                table: "GanjoorQuotedPoems");
        }
    }
}
