using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorHalfCenturies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthYearInLHijri",
                table: "GanjoorPoets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeathYearInLHijri",
                table: "GanjoorPoets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GanjoorHalfCenturyId",
                table: "GanjoorPoets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GanjoorHalfCenturies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HalfCenturyOrder = table.Column<int>(type: "int", nullable: false),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    EndYear = table.Column<int>(type: "int", nullable: false),
                    ShowInTimeLine = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorHalfCenturies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoets_GanjoorHalfCenturyId",
                table: "GanjoorPoets",
                column: "GanjoorHalfCenturyId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorPoets_GanjoorHalfCenturies_GanjoorHalfCenturyId",
                table: "GanjoorPoets",
                column: "GanjoorHalfCenturyId",
                principalTable: "GanjoorHalfCenturies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorPoets_GanjoorHalfCenturies_GanjoorHalfCenturyId",
                table: "GanjoorPoets");

            migrationBuilder.DropTable(
                name: "GanjoorHalfCenturies");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoets_GanjoorHalfCenturyId",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "BirthYearInLHijri",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "DeathYearInLHijri",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "GanjoorHalfCenturyId",
                table: "GanjoorPoets");
        }
    }
}
