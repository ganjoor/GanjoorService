using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class PoemMusicTrackRejected : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Rejected",
                table: "GanjoorPoemMusicTracks",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionCause",
                table: "GanjoorPoemMusicTracks",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rejected",
                table: "GanjoorPoemMusicTracks");

            migrationBuilder.DropColumn(
                name: "RejectionCause",
                table: "GanjoorPoemMusicTracks");
        }
    }
}
