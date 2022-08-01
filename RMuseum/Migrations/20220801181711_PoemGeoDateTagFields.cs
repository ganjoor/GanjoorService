using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class PoemGeoDateTagFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PoemGeoDateTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    LunarYear = table.Column<int>(type: "int", nullable: true),
                    LunarMonth = table.Column<int>(type: "int", nullable: true),
                    LunarDay = table.Column<int>(type: "int", nullable: true),
                    LunarDateTotalNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemGeoDateTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoemGeoDateTags_GanjoorGeoLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "GanjoorGeoLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PoemGeoDateTags_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoemGeoDateTags_LocationId",
                table: "PoemGeoDateTags",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemGeoDateTags_PoemId",
                table: "PoemGeoDateTags",
                column: "PoemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoemGeoDateTags");
        }
    }
}
