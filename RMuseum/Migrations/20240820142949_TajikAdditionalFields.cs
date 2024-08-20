using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class TajikAdditionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthYearInLHijri",
                table: "TajikPoets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullTitle",
                table: "TajikPoems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullUrl",
                table: "TajikPoems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PoetId",
                table: "TajikCats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TajikPoems_CatId",
                table: "TajikPoems",
                column: "CatId");

            migrationBuilder.CreateIndex(
                name: "IX_TajikCats_PoetId",
                table: "TajikCats",
                column: "PoetId");

            migrationBuilder.AddForeignKey(
                name: "FK_TajikCats_TajikPoets_PoetId",
                table: "TajikCats",
                column: "PoetId",
                principalTable: "TajikPoets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TajikPoems_TajikCats_CatId",
                table: "TajikPoems",
                column: "CatId",
                principalTable: "TajikCats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TajikCats_TajikPoets_PoetId",
                table: "TajikCats");

            migrationBuilder.DropForeignKey(
                name: "FK_TajikPoems_TajikCats_CatId",
                table: "TajikPoems");

            migrationBuilder.DropIndex(
                name: "IX_TajikPoems_CatId",
                table: "TajikPoems");

            migrationBuilder.DropIndex(
                name: "IX_TajikCats_PoetId",
                table: "TajikCats");

            migrationBuilder.DropColumn(
                name: "BirthYearInLHijri",
                table: "TajikPoets");

            migrationBuilder.DropColumn(
                name: "FullTitle",
                table: "TajikPoems");

            migrationBuilder.DropColumn(
                name: "FullUrl",
                table: "TajikPoems");

            migrationBuilder.DropColumn(
                name: "PoetId",
                table: "TajikCats");
        }
    }
}
