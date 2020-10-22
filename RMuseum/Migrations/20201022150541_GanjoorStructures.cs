using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorStructures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoets",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorCategories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    PoetId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    ParentId = table.Column<int>(nullable: true),
                    UrlSlug = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCategories_GanjoorCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorCategories_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorPoems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    CatId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    FullTitle = table.Column<string>(nullable: true),
                    UrlSlug = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoems_GanjoorCategories_CatId",
                        column: x => x.CatId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorVerses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(nullable: false),
                    VOrder = table.Column<int>(nullable: false),
                    VersePosition = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorVerses_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_ParentId",
                table: "GanjoorCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_PoetId",
                table: "GanjoorCategories",
                column: "PoetId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoems_CatId",
                table: "GanjoorPoems",
                column: "CatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerses_PoemId",
                table: "GanjoorVerses",
                column: "PoemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorVerses");

            migrationBuilder.DropTable(
                name: "GanjoorPoems");

            migrationBuilder.DropTable(
                name: "GanjoorCategories");

            migrationBuilder.DropTable(
                name: "GanjoorPoets");
        }
    }
}
