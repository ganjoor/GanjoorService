using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class PoetNewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "GanjoorPoets",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "GanjoorPoets",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RImageId",
                table: "GanjoorPoets",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoets_RImageId",
                table: "GanjoorPoets",
                column: "RImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorPoets_GeneralImages_RImageId",
                table: "GanjoorPoets",
                column: "RImageId",
                principalTable: "GeneralImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorPoets_GeneralImages_RImageId",
                table: "GanjoorPoets");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoets_RImageId",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "RImageId",
                table: "GanjoorPoets");
        }
    }
}
