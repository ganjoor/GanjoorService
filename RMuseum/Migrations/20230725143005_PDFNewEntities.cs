using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class PDFNewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookScriptType",
                table: "PDFBooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PDFSourceId",
                table: "PDFBooks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PDFSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PDFBooks_PDFSourceId",
                table: "PDFBooks",
                column: "PDFSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_PDFBooks_PDFSources_PDFSourceId",
                table: "PDFBooks",
                column: "PDFSourceId",
                principalTable: "PDFSources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PDFBooks_PDFSources_PDFSourceId",
                table: "PDFBooks");

            migrationBuilder.DropTable(
                name: "PDFSources");

            migrationBuilder.DropIndex(
                name: "IX_PDFBooks_PDFSourceId",
                table: "PDFBooks");

            migrationBuilder.DropColumn(
                name: "BookScriptType",
                table: "PDFBooks");

            migrationBuilder.DropColumn(
                name: "PDFSourceId",
                table: "PDFBooks");
        }
    }
}
