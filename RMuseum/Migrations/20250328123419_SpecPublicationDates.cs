using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class SpecPublicationDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublicationDate",
                table: "GanjoorPoetSuggestedSpecLines",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicationDate",
                table: "GanjoorPoetSuggestedPictures",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicationDate",
                table: "GanjoorPoetSuggestedSpecLines");

            migrationBuilder.DropColumn(
                name: "PublicationDate",
                table: "GanjoorPoetSuggestedPictures");
        }
    }
}
