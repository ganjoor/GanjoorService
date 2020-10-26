using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class AudioFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioFiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<Guid>(nullable: false),
                    GanjoorAudioId = table.Column<int>(nullable: false),
                    GanjoorPostId = table.Column<int>(nullable: false),
                    AudioOrder = table.Column<int>(nullable: false),
                    FileNameWithoutExtension = table.Column<string>(nullable: true),
                    SoundFilesFolder = table.Column<string>(nullable: true),
                    AudioTitle = table.Column<string>(nullable: true),
                    AudioArtist = table.Column<string>(nullable: true),
                    AudioArtistUrl = table.Column<string>(nullable: true),
                    AudioSrc = table.Column<string>(nullable: true),
                    AudioSrcUrl = table.Column<string>(nullable: true),
                    LegacyAudioGuid = table.Column<Guid>(nullable: false),
                    Mp3FileCheckSum = table.Column<string>(nullable: true),
                    Mp3SizeInBytes = table.Column<int>(nullable: false),
                    OggSizeInBytes = table.Column<int>(nullable: false),
                    UploadDate = table.Column<DateTime>(nullable: false),
                    ReviewDate = table.Column<DateTime>(nullable: false),
                    LocalMp3FilePath = table.Column<string>(nullable: true),
                    LocalXmlFilePath = table.Column<string>(nullable: true),
                    AudioSyncStatus = table.Column<int>(nullable: false),
                    ReviewStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioFiles_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_OwnerId",
                table: "AudioFiles",
                column: "OwnerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioFiles");
        }
    }
}
