using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class TajikFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TajikHtmlText",
                table: "TajikPoems");

            migrationBuilder.CreateTable(
                name: "TajikPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TajikHtmlText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TajikPages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TajikPages");

            migrationBuilder.AddColumn<string>(
                name: "TajikHtmlText",
                table: "TajikPoems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
