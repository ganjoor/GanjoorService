using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class DuplicatesDbModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorDuplicates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SrcCatId = table.Column<int>(type: "int", nullable: false),
                    SrcPoemId = table.Column<int>(type: "int", nullable: false),
                    DestPoemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorDuplicates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorDuplicates_GanjoorPoems_DestPoemId",
                        column: x => x.DestPoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorDuplicates_GanjoorPoems_SrcPoemId",
                        column: x => x.SrcPoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorDuplicates_DestPoemId",
                table: "GanjoorDuplicates",
                column: "DestPoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorDuplicates_SrcPoemId",
                table: "GanjoorDuplicates",
                column: "SrcPoemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorDuplicates");
        }
    }
}
