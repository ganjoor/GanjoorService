using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorGeoLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthLocationId",
                table: "GanjoorPoets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeathLocationId",
                table: "GanjoorPoets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ValidBirthDate",
                table: "GanjoorPoets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ValidDeathDate",
                table: "GanjoorPoets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GanjoorGeoLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(12,9)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(12,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorGeoLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoets_BirthLocationId",
                table: "GanjoorPoets",
                column: "BirthLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoets_DeathLocationId",
                table: "GanjoorPoets",
                column: "DeathLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorPoets_GanjoorGeoLocations_BirthLocationId",
                table: "GanjoorPoets",
                column: "BirthLocationId",
                principalTable: "GanjoorGeoLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorPoets_GanjoorGeoLocations_DeathLocationId",
                table: "GanjoorPoets",
                column: "DeathLocationId",
                principalTable: "GanjoorGeoLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorPoets_GanjoorGeoLocations_BirthLocationId",
                table: "GanjoorPoets");

            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorPoets_GanjoorGeoLocations_DeathLocationId",
                table: "GanjoorPoets");

            migrationBuilder.DropTable(
                name: "GanjoorGeoLocations");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoets_BirthLocationId",
                table: "GanjoorPoets");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoets_DeathLocationId",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "BirthLocationId",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "DeathLocationId",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "ValidBirthDate",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "ValidDeathDate",
                table: "GanjoorPoets");
        }
    }
}
