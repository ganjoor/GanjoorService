using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class NewTajikEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TajikCats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TajikTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TajikDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TajikCats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TajikPoems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CatId = table.Column<int>(type: "int", nullable: false),
                    TajikTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TajikPlainText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TajikHtmlText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TajikPoems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TajikPoets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    TajikNickname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TajikDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TajikPoets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TajikVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    VOrder = table.Column<int>(type: "int", nullable: false),
                    TajikText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TajikVerses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TajikCats");

            migrationBuilder.DropTable(
                name: "TajikPoems");

            migrationBuilder.DropTable(
                name: "TajikPoets");

            migrationBuilder.DropTable(
                name: "TajikVerses");
        }
    }
}
