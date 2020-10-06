using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class ReviewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewMsg",
                table: "AudioFiles",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerId",
                table: "AudioFiles",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_ReviewerId",
                table: "AudioFiles",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioFiles_AspNetUsers_ReviewerId",
                table: "AudioFiles",
                column: "ReviewerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioFiles_AspNetUsers_ReviewerId",
                table: "AudioFiles");

            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_ReviewerId",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "ReviewMsg",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "AudioFiles");
        }
    }
}
