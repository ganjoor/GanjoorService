using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class RArtifactMasterRecordCSIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_CoverItemIndex_Status",
                table: "Artifacts",
                columns: new[] { "CoverItemIndex", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artifacts_CoverItemIndex_Status",
                table: "Artifacts");
        }
    }
}
