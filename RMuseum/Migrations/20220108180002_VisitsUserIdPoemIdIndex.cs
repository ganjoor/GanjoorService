using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class VisitsUserIdPoemIdIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GanjoorUserPoemVisits_UserId_PoemId",
                table: "GanjoorUserPoemVisits",
                columns: new[] { "UserId", "PoemId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorUserPoemVisits_UserId_PoemId",
                table: "GanjoorUserPoemVisits");
        }
    }
}
