using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class Recitations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoets",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recitations",
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
                    FileLastUpdated = table.Column<DateTime>(nullable: false),
                    ReviewDate = table.Column<DateTime>(nullable: false),
                    LocalMp3FilePath = table.Column<string>(nullable: true),
                    LocalXmlFilePath = table.Column<string>(nullable: true),
                    AudioSyncStatus = table.Column<int>(nullable: false),
                    ReviewStatus = table.Column<int>(nullable: false),
                    ReviewerId = table.Column<Guid>(nullable: true),
                    ReviewMsg = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recitations_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recitations_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SessionType = table.Column<int>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    UseId = table.Column<Guid>(nullable: false),
                    UploadStartTime = table.Column<DateTime>(nullable: false),
                    UploadEndTime = table.Column<DateTime>(nullable: false),
                    ProcessStartTime = table.Column<DateTime>(nullable: false),
                    ProcessEndTime = table.Column<DateTime>(nullable: false),
                    ProcessProgress = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRecitationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    FileSuffixWithoutDash = table.Column<string>(nullable: true),
                    ArtistName = table.Column<string>(nullable: true),
                    ArtistUrl = table.Column<string>(nullable: true),
                    AudioSrc = table.Column<string>(nullable: true),
                    AudioSrcUrl = table.Column<string>(nullable: true),
                    IsDefault = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecitationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRecitationProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorCategories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    PoetId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    ParentId = table.Column<int>(nullable: true),
                    UrlSlug = table.Column<string>(nullable: true),
                    FullUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorCategories_GanjoorCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorCategories_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecitationPublishingTrackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PoemNarrationId = table.Column<int>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: false),
                    XmlFileCopied = table.Column<bool>(nullable: false),
                    Mp3FileCopied = table.Column<bool>(nullable: false),
                    FirstDbUpdated = table.Column<bool>(nullable: false),
                    SecondDbUpdated = table.Column<bool>(nullable: false),
                    Finished = table.Column<bool>(nullable: false),
                    FinishDate = table.Column<DateTime>(nullable: false),
                    LastException = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecitationPublishingTrackers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecitationPublishingTrackers_Recitations_PoemNarrationId",
                        column: x => x.PoemNarrationId,
                        principalTable: "Recitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UploadSessionId = table.Column<Guid>(nullable: false),
                    ContentDisposition = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    FileName = table.Column<string>(nullable: true),
                    Length = table.Column<long>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    FilePath = table.Column<string>(nullable: true),
                    MP3FileCheckSum = table.Column<string>(nullable: true),
                    ProcessResult = table.Column<bool>(nullable: false),
                    ProcessResultMsg = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorPoems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    CatId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    FullTitle = table.Column<string>(nullable: true),
                    UrlSlug = table.Column<string>(nullable: true),
                    FullUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoems_GanjoorCategories_CatId",
                        column: x => x.CatId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorVerses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(nullable: false),
                    VOrder = table.Column<int>(nullable: false),
                    VersePosition = table.Column<int>(nullable: false),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorVerses_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_FullUrl",
                table: "GanjoorCategories",
                column: "FullUrl");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_ParentId",
                table: "GanjoorCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_PoetId",
                table: "GanjoorCategories",
                column: "PoetId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoems_CatId",
                table: "GanjoorPoems",
                column: "CatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoems_FullUrl",
                table: "GanjoorPoems",
                column: "FullUrl");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorVerses_PoemId",
                table: "GanjoorVerses",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecitationPublishingTrackers_PoemNarrationId",
                table: "RecitationPublishingTrackers",
                column: "PoemNarrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Recitations_GanjoorPostId",
                table: "Recitations",
                column: "GanjoorPostId");

            migrationBuilder.CreateIndex(
                name: "IX_Recitations_OwnerId",
                table: "Recitations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Recitations_ReviewerId",
                table: "Recitations",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UploadSessionId",
                table: "UploadedFiles",
                column: "UploadSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_UserId",
                table: "UploadSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRecitationProfiles_UserId",
                table: "UserRecitationProfiles",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorVerses");

            migrationBuilder.DropTable(
                name: "RecitationPublishingTrackers");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.DropTable(
                name: "UserRecitationProfiles");

            migrationBuilder.DropTable(
                name: "GanjoorPoems");

            migrationBuilder.DropTable(
                name: "Recitations");

            migrationBuilder.DropTable(
                name: "UploadSessions");

            migrationBuilder.DropTable(
                name: "GanjoorCategories");

            migrationBuilder.DropTable(
                name: "GanjoorPoets");
        }
    }
}
