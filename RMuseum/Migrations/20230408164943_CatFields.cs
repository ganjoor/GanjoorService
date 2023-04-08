using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class CatFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookName",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapName",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RImageId",
                table: "GanjoorCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SumUpSubsGeoLocations",
                table: "GanjoorCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_RImageId",
                table: "GanjoorCategories",
                column: "RImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorCategories_GeneralImages_RImageId",
                table: "GanjoorCategories",
                column: "RImageId",
                principalTable: "GeneralImages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorCategories_GeneralImages_RImageId",
                table: "GanjoorCategories");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorCategories_RImageId",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "BookName",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "MapName",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "RImageId",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "SumUpSubsGeoLocations",
                table: "GanjoorCategories");
        }
    }
}
