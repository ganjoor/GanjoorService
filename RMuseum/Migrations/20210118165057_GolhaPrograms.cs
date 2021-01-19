using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GolhaPrograms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GolhaProgram_GolhaCollections_GolhaCollectionId",
                table: "GolhaProgram");

            migrationBuilder.DropForeignKey(
                name: "FK_GolhaTracks_GolhaProgram_GolhaProgramId",
                table: "GolhaTracks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GolhaProgram",
                table: "GolhaProgram");

            migrationBuilder.RenameTable(
                name: "GolhaProgram",
                newName: "GolhaPrograms");

            migrationBuilder.RenameIndex(
                name: "IX_GolhaProgram_GolhaCollectionId",
                table: "GolhaPrograms",
                newName: "IX_GolhaPrograms_GolhaCollectionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GolhaPrograms",
                table: "GolhaPrograms",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GolhaPrograms_GolhaCollections_GolhaCollectionId",
                table: "GolhaPrograms",
                column: "GolhaCollectionId",
                principalTable: "GolhaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GolhaTracks_GolhaPrograms_GolhaProgramId",
                table: "GolhaTracks",
                column: "GolhaProgramId",
                principalTable: "GolhaPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GolhaPrograms_GolhaCollections_GolhaCollectionId",
                table: "GolhaPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_GolhaTracks_GolhaPrograms_GolhaProgramId",
                table: "GolhaTracks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GolhaPrograms",
                table: "GolhaPrograms");

            migrationBuilder.RenameTable(
                name: "GolhaPrograms",
                newName: "GolhaProgram");

            migrationBuilder.RenameIndex(
                name: "IX_GolhaPrograms_GolhaCollectionId",
                table: "GolhaProgram",
                newName: "IX_GolhaProgram_GolhaCollectionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GolhaProgram",
                table: "GolhaProgram",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GolhaProgram_GolhaCollections_GolhaCollectionId",
                table: "GolhaProgram",
                column: "GolhaCollectionId",
                principalTable: "GolhaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GolhaTracks_GolhaProgram_GolhaProgramId",
                table: "GolhaTracks",
                column: "GolhaProgramId",
                principalTable: "GolhaProgram",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
