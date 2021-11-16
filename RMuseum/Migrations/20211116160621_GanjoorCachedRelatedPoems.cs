using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorCachedRelatedPoems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorCachedRelatedPoems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    PoetId = table.Column<int>(type: "int", nullable: false),
                    RelationOrder = table.Column<int>(type: "int", nullable: false),
                    PoetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoetImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HtmlExcerpt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoetMorePoemsLikeThisCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCachedRelatedPoems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCachedRelatedPoems_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCachedRelatedPoems_PoemId",
                table: "GanjoorCachedRelatedPoems",
                column: "PoemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorCachedRelatedPoems");
        }
    }
}
