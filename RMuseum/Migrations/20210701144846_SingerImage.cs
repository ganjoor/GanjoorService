using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class SingerImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RImageId",
                table: "GanjoorSingers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorSingers_RImageId",
                table: "GanjoorSingers",
                column: "RImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorSingers_GeneralImages_RImageId",
                table: "GanjoorSingers",
                column: "RImageId",
                principalTable: "GeneralImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorSingers_GeneralImages_RImageId",
                table: "GanjoorSingers");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorSingers_RImageId",
                table: "GanjoorSingers");

            migrationBuilder.DropColumn(
                name: "RImageId",
                table: "GanjoorSingers");
        }
    }
}
