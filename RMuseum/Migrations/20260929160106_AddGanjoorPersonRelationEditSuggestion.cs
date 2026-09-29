using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddGanjoorPersonRelationEditSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SuggestedForDeletion",
                table: "GanjoorPersonEditSuggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GanjoorPersonRelationEditSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ExistingRelationId = table.Column<int>(type: "int", nullable: true),
                    Person1Id = table.Column<int>(type: "int", nullable: false),
                    Person2Id = table.Column<int>(type: "int", nullable: false),
                    SuggestedRelationType = table.Column<int>(type: "int", nullable: false),
                    SuggestedDegreeHint = table.Column<int>(type: "int", nullable: true),
                    SuggestedNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reviewed = table.Column<bool>(type: "bit", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPersonRelationEditSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelationEditSuggestions_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelationEditSuggestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelationEditSuggestions_GanjoorPersonRelations_ExistingRelationId",
                        column: x => x.ExistingRelationId,
                        principalTable: "GanjoorPersonRelations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelationEditSuggestions_GanjoorRelatedPersons_Person1Id",
                        column: x => x.Person1Id,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelationEditSuggestions_GanjoorRelatedPersons_Person2Id",
                        column: x => x.Person2Id,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_ExistingRelationId",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "ExistingRelationId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_Person1Id",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "Person1Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_Person2Id",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "Person2Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_ReviewerUserId",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEditSuggestions_UserId",
                table: "GanjoorPersonRelationEditSuggestions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "SuggestedForDeletion",
                table: "GanjoorPersonEditSuggestions");
        }
    }
}
