using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorHealthCheckError : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorHealthCheckErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferrerPageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrokenLink = table.Column<bool>(type: "bit", nullable: false),
                    MulipleTargets = table.Column<bool>(type: "bit", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorHealthCheckErrors", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorHealthCheckErrors");
        }
    }
}
