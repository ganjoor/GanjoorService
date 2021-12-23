using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class RecitationErrorReportNewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoupletIndex",
                table: "RecitationErrorReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfLinesAffected",
                table: "RecitationErrorReports",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoupletIndex",
                table: "RecitationErrorReports");

            migrationBuilder.DropColumn(
                name: "NumberOfLinesAffected",
                table: "RecitationErrorReports");
        }
    }
}
