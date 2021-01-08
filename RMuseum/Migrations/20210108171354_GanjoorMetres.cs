using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorMetres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GanjoorMetreId",
                table: "GanjoorPoems",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GanjoorMetres",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UrlSlug = table.Column<string>(nullable: true),
                    Rhythm = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorMetres", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoems_GanjoorMetreId",
                table: "GanjoorPoems",
                column: "GanjoorMetreId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorPoems_GanjoorMetres_GanjoorMetreId",
                table: "GanjoorPoems",
                column: "GanjoorMetreId",
                principalTable: "GanjoorMetres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorPoems_GanjoorMetres_GanjoorMetreId",
                table: "GanjoorPoems");

            migrationBuilder.DropTable(
                name: "GanjoorMetres");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoems_GanjoorMetreId",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "GanjoorMetreId",
                table: "GanjoorPoems");
        }
    }
}
