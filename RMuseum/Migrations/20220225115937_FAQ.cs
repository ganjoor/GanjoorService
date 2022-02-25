using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class FAQ : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FAQCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CatOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FAQItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswerExcerpt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pinned = table.Column<bool>(type: "bit", nullable: false),
                    PinnedItemOrder = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ItemOrderInCategory = table.Column<int>(type: "int", nullable: false),
                    ContentForSearch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashTag1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashTag2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashTag3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HashTag4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FAQItems_FAQCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FAQCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FAQItems_CategoryId",
                table: "FAQItems",
                column: "CategoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FAQItems");

            migrationBuilder.DropTable(
                name: "FAQCategories");
        }
    }
}
