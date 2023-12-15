using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class QPRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GanjoorQuotedPoems_PoemId",
                table: "GanjoorQuotedPoems",
                column: "PoemId");

            migrationBuilder.AddForeignKey(
                name: "FK_GanjoorQuotedPoems_GanjoorPoems_PoemId",
                table: "GanjoorQuotedPoems",
                column: "PoemId",
                principalTable: "GanjoorPoems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GanjoorQuotedPoems_GanjoorPoems_PoemId",
                table: "GanjoorQuotedPoems");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorQuotedPoems_PoemId",
                table: "GanjoorQuotedPoems");
        }
    }
}
