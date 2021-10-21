using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorCenturies : Migration
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
                name: "PinOrder",
                table: "GanjoorPoets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GanjoorCenturies",
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
                    table.PrimaryKey("PK_GanjoorCenturies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorCenturyPoet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoetOrder = table.Column<int>(type: "int", nullable: false),
                    PoetId = table.Column<int>(type: "int", nullable: true),
                    GanjoorCenturyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCenturyPoet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCenturyPoet_GanjoorCenturies_GanjoorCenturyId",
                        column: x => x.GanjoorCenturyId,
                        principalTable: "GanjoorCenturies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorCenturyPoet_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCenturyPoet_GanjoorCenturyId",
                table: "GanjoorCenturyPoet",
                column: "GanjoorCenturyId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCenturyPoet_PoetId",
                table: "GanjoorCenturyPoet",
                column: "PoetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorCenturyPoet");

            migrationBuilder.DropTable(
                name: "GanjoorCenturies");

            migrationBuilder.DropColumn(
                name: "BirthYearInLHijri",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "DeathYearInLHijri",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "PinOrder",
                table: "GanjoorPoets");
        }
    }
}
