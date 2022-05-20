using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class UpdtRelSectsLogModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse10VOrder",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse10VOrderResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse5VOrder",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse5VOrderResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse6VOrder",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse6VOrderResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse7VOrder",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse7VOrderResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse8VOrder",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse8VOrderResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse9VOrder",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakFromVerse9VOrderResult",
                table: "GanjoorPoemSectionCorrections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UpdatingRelSectsLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeterId = table.Column<int>(type: "int", nullable: false),
                    RhymeLettes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdatingRelSectsLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdatingRelSectsLogs");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse10VOrder",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse10VOrderResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse5VOrder",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse5VOrderResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse6VOrder",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse6VOrderResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse7VOrder",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse7VOrderResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse8VOrder",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse8VOrderResult",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse9VOrder",
                table: "GanjoorPoemSectionCorrections");

            migrationBuilder.DropColumn(
                name: "BreakFromVerse9VOrderResult",
                table: "GanjoorPoemSectionCorrections");
        }
    }
}
