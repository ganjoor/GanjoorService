using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class AddPoemGeoSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoemGeoDateTagCorrection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoupletIndex = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    SuggestedLocationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedLatitude = table.Column<double>(type: "float", nullable: true),
                    SuggestedLongitude = table.Column<double>(type: "float", nullable: true),
                    LunarYear = table.Column<int>(type: "int", nullable: true),
                    LunarMonth = table.Column<int>(type: "int", nullable: true),
                    LunarDay = table.Column<int>(type: "int", nullable: true),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    IgnoreInCategory = table.Column<bool>(type: "bit", nullable: false),
                    SuggestionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarkForDelete = table.Column<bool>(type: "bit", nullable: false),
                    ExistingTagId = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GanjoorPoemCorrectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoemGeoDateTagCorrection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemGeoDateTagCorrection_GanjoorGeoLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GanjoorGeoLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoemGeoDateTagCorrection_GanjoorPoemCorrections_GanjoorPoemCorrectionId",
                        column: x => x.GanjoorPoemCorrectionId,
                        principalTable: "GanjoorPoemCorrections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoemGeoDateTagCorrection_GanjoorRelatedPersons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "GanjoorRelatedPersons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemGeoDateTagCorrection_GanjoorPoemCorrectionId",
                table: "GanjoorPoemGeoDateTagCorrection",
                column: "GanjoorPoemCorrectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemGeoDateTagCorrection_LocationId",
                table: "GanjoorPoemGeoDateTagCorrection",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemGeoDateTagCorrection_PersonId",
                table: "GanjoorPoemGeoDateTagCorrection",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoemGeoDateTagCorrection");
        }
    }
}
