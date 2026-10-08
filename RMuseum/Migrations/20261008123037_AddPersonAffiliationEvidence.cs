using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonAffiliationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPersonAffiliationEvidences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AffiliationId = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    CoupletText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MasterCatId = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPersonAffiliationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonAffiliationEvidences_GanjoorPersonAffiliations_AffiliationId",
                        column: x => x.AffiliationId,
                        principalTable: "GanjoorPersonAffiliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonAffiliationEvidences_AffiliationId_PoemId_CoupletIndex",
                table: "GanjoorPersonAffiliationEvidences",
                columns: new[] { "AffiliationId", "PoemId", "CoupletIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonAffiliationEvidences_MasterCatId",
                table: "GanjoorPersonAffiliationEvidences",
                column: "MasterCatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPersonAffiliationEvidences");
        }
    }
}
