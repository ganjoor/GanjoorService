using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class OCR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FullResolutionImageHeight",
                table: "PDFPages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FullResolutionImageWidth",
                table: "PDFPages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OCRTime",
                table: "PDFPages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "OCRed",
                table: "PDFPages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PageText",
                table: "PDFPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OCRTime",
                table: "PDFBooks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "OCRed",
                table: "PDFBooks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullResolutionImageHeight",
                table: "PDFPages");

            migrationBuilder.DropColumn(
                name: "FullResolutionImageWidth",
                table: "PDFPages");

            migrationBuilder.DropColumn(
                name: "OCRTime",
                table: "PDFPages");

            migrationBuilder.DropColumn(
                name: "OCRed",
                table: "PDFPages");

            migrationBuilder.DropColumn(
                name: "PageText",
                table: "PDFPages");

            migrationBuilder.DropColumn(
                name: "OCRTime",
                table: "PDFBooks");

            migrationBuilder.DropColumn(
                name: "OCRed",
                table: "PDFBooks");
        }
    }
}
