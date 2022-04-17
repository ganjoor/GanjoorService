using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class SectionIndexInCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetPoemId",
                table: "GanjoorCachedRelatedSections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TargetSectionIndex",
                table: "GanjoorCachedRelatedSections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetPoemId",
                table: "GanjoorCachedRelatedSections");

            migrationBuilder.DropColumn(
                name: "TargetSectionIndex",
                table: "GanjoorCachedRelatedSections");
        }
    }
}
