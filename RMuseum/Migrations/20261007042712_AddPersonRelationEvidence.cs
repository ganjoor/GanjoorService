using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonRelationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EvidenceCoupletIndex",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceCoupletText",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidencePoemId",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExistingEvidenceId",
                table: "GanjoorPersonRelationEditSuggestions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GanjoorPersonRelationEvidences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RelationId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    CoupletText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MasterCatId = table.Column<int>(type: "int", nullable: false),
                    Inferred = table.Column<bool>(type: "bit", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPersonRelationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelationEvidences_GanjoorPersonRelations_RelationId",
                        column: x => x.RelationId,
                        principalTable: "GanjoorPersonRelations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEvidences_MasterCatId",
                table: "GanjoorPersonRelationEvidences",
                column: "MasterCatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelationEvidences_RelationId_PoemId_CoupletIndex",
                table: "GanjoorPersonRelationEvidences",
                columns: new[] { "RelationId", "PoemId", "CoupletIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPersonRelationEvidences");

            migrationBuilder.DropColumn(
                name: "EvidenceCoupletIndex",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "EvidenceCoupletText",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "EvidencePoemId",
                table: "GanjoorPersonRelationEditSuggestions");

            migrationBuilder.DropColumn(
                name: "ExistingEvidenceId",
                table: "GanjoorPersonRelationEditSuggestions");
        }
    }
}
