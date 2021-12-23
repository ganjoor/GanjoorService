using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class RecitationMistakes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecitationApprovedMistakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecitationId = table.Column<int>(type: "int", nullable: false),
                    Mistake = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfLinesAffected = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecitationApprovedMistakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecitationApprovedMistakes_Recitations_RecitationId",
                        column: x => x.RecitationId,
                        principalTable: "Recitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecitationApprovedMistakes_RecitationId",
                table: "RecitationApprovedMistakes",
                column: "RecitationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecitationApprovedMistakes");
        }
    }
}
