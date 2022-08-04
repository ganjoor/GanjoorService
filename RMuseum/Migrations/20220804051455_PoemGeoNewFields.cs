using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class PoemGeoNewFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInCategory",
                table: "PoemGeoDateTags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VerifiedDate",
                table: "PoemGeoDateTags",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreInCategory",
                table: "PoemGeoDateTags");

            migrationBuilder.DropColumn(
                name: "VerifiedDate",
                table: "PoemGeoDateTags");
        }
    }
}
