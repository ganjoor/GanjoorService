using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class People : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonId",
                table: "PoemGeoDateTags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GanjoorRelatedPersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WikiUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthYearInLHijri = table.Column<int>(type: "int", nullable: false),
                    DeathYearInLHijri = table.Column<int>(type: "int", nullable: false),
                    ValidBirthDate = table.Column<bool>(type: "bit", nullable: false),
                    ValidDeathDate = table.Column<bool>(type: "bit", nullable: false),
                    BirthLocationId = table.Column<int>(type: "int", nullable: true),
                    DeathLocationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorRelatedPersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorRelatedPersons_GanjoorGeoLocations_BirthLocationId",
                        column: x => x.BirthLocationId,
                        principalTable: "GanjoorGeoLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorRelatedPersons_GanjoorGeoLocations_DeathLocationId",
                        column: x => x.DeathLocationId,
                        principalTable: "GanjoorGeoLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoemGeoDateTags_PersonId",
                table: "PoemGeoDateTags",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorRelatedPersons_BirthLocationId",
                table: "GanjoorRelatedPersons",
                column: "BirthLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorRelatedPersons_DeathLocationId",
                table: "GanjoorRelatedPersons",
                column: "DeathLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PoemGeoDateTags_GanjoorRelatedPersons_PersonId",
                table: "PoemGeoDateTags",
                column: "PersonId",
                principalTable: "GanjoorRelatedPersons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PoemGeoDateTags_GanjoorRelatedPersons_PersonId",
                table: "PoemGeoDateTags");

            migrationBuilder.DropTable(
                name: "GanjoorRelatedPersons");

            migrationBuilder.DropIndex(
                name: "IX_PoemGeoDateTags_PersonId",
                table: "PoemGeoDateTags");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "PoemGeoDateTags");
        }
    }
}
