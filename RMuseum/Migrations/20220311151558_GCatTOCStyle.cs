using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class GCatTOCStyle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "GanjoorPoems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MixedModeOrder",
                table: "GanjoorPoems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "GanjoorPoems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoIndex",
                table: "GanjoorPages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RedirectFromFullUrl",
                table: "GanjoorPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatType",
                table: "GanjoorCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionHtml",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MixedModeOrder",
                table: "GanjoorCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "GanjoorCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TableOfContentsStyle",
                table: "GanjoorCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "MixedModeOrder",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "NoIndex",
                table: "GanjoorPages");

            migrationBuilder.DropColumn(
                name: "RedirectFromFullUrl",
                table: "GanjoorPages");

            migrationBuilder.DropColumn(
                name: "CatType",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "DescriptionHtml",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "MixedModeOrder",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "TableOfContentsStyle",
                table: "GanjoorCategories");
        }
    }
}
