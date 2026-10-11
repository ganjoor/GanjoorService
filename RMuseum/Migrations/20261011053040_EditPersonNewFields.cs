using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class EditPersonNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MergeIntoPersonId",
                table: "GanjoorPersonEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Operation",
                table: "GanjoorPersonEditSuggestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResultReport",
                table: "GanjoorPersonEditSuggestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitFromCoupletIndex",
                table: "GanjoorPersonEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitFromPoemId",
                table: "GanjoorPersonEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitNewDescription",
                table: "GanjoorPersonEditSuggestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitNewName",
                table: "GanjoorPersonEditSuggestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitToCoupletIndex",
                table: "GanjoorPersonEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitToPoemId",
                table: "GanjoorPersonEditSuggestions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MergeIntoPersonId",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "Operation",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "ResultReport",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SplitFromCoupletIndex",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SplitFromPoemId",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SplitNewDescription",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SplitNewName",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SplitToCoupletIndex",
                table: "GanjoorPersonEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SplitToPoemId",
                table: "GanjoorPersonEditSuggestions");
        }
    }
}
