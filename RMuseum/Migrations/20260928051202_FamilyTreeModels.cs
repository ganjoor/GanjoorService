using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class FamilyTreeModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FamilyTreeCaption",
                table: "GanjoorRelatedPersons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedPersonGraphJson",
                table: "GanjoorPoemGeoDateTagCorrection",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GanjoorPersonAffiliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Person1Id = table.Column<int>(type: "int", nullable: false),
                    Person2Id = table.Column<int>(type: "int", nullable: false),
                    AffiliationType = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPersonAffiliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonAffiliations_GanjoorRelatedPersons_Person1Id",
                        column: x => x.Person1Id,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonAffiliations_GanjoorRelatedPersons_Person2Id",
                        column: x => x.Person2Id,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorPersonRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Person1Id = table.Column<int>(type: "int", nullable: false),
                    Person2Id = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    DegreeHint = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPersonRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelations_GanjoorRelatedPersons_Person1Id",
                        column: x => x.Person1Id,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPersonRelations_GanjoorRelatedPersons_Person2Id",
                        column: x => x.Person2Id,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonAffiliations_Person1Id",
                table: "GanjoorPersonAffiliations",
                column: "Person1Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonAffiliations_Person2Id",
                table: "GanjoorPersonAffiliations",
                column: "Person2Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelations_Person1Id",
                table: "GanjoorPersonRelations",
                column: "Person1Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPersonRelations_Person2Id",
                table: "GanjoorPersonRelations",
                column: "Person2Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPersonAffiliations");

            migrationBuilder.DropTable(
                name: "GanjoorPersonRelations");

            migrationBuilder.DropColumn(
                name: "FamilyTreeCaption",
                table: "GanjoorRelatedPersons");

            migrationBuilder.DropColumn(
                name: "SuggestedPersonGraphJson",
                table: "GanjoorPoemGeoDateTagCorrection");
        }
    }
}
