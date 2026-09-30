using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonRelationSuggestionAffiliationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExistingAffiliationId",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedAffiliationType",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_ExistingAffiliationId",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "ExistingAffiliationId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorPersonRelationEditSuggestions_GanjoorPersonAffiliations_ExistingAffiliationId",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "ExistingAffiliationId",
                principalTable: "GanjoorPersonAffiliations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorPersonRelationEditSuggestions_GanjoorPersonAffiliations_ExistingAffiliationId",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_ExistingAffiliationId",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "ExistingAffiliationId",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SuggestedAffiliationType",
                table: "GanjoorPersonRelationEditSuggestions");
        }
    }
}
