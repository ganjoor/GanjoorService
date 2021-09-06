using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorCommentVerse12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Verse12d",
                table: "GanjoorComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Verse1Id",
                table: "GanjoorComments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Verse2Id",
                table: "GanjoorComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_Verse1Id",
                table: "GanjoorComments",
                column: "Verse1Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_Verse2Id",
                table: "GanjoorComments",
                column: "Verse2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorComments_GanjoorVerses_Verse1Id",
                table: "GanjoorComments",
                column: "Verse1Id",
                principalTable: "GanjoorVerses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorComments_GanjoorVerses_Verse2Id",
                table: "GanjoorComments",
                column: "Verse2Id",
                principalTable: "GanjoorVerses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorComments_GanjoorVerses_Verse1Id",
                table: "GanjoorComments");

            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorComments_GanjoorVerses_Verse2Id",
                table: "GanjoorComments");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorComments_Verse1Id",
                table: "GanjoorComments");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorComments_Verse2Id",
                table: "GanjoorComments");

            migrationBuilder.DropColumn(
                name: "Verse12d",
                table: "GanjoorComments");

            migrationBuilder.DropColumn(
                name: "Verse1Id",
                table: "GanjoorComments");

            migrationBuilder.DropColumn(
                name: "Verse2Id",
                table: "GanjoorComments");
        }
    }
}
