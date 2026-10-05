using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonSuggestionConfirmationOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedExtraParent",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmedDuplicateFamilyTreeCaption",
                table: "GanjoorPersonEditSuggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedExtraParent",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "ConfirmedDuplicateFamilyTreeCaption",
                table: "GanjoorPersonEditSuggestions");
        }
    }
}
