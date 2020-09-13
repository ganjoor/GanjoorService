using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class UploadNarration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "UserNarrationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    FileSuffixWithoutDash = table.Column<string>(nullable: true),
                    ArtistName = table.Column<string>(nullable: true),
                    ArtistUrl = table.Column<string>(nullable: true),
                    AudioSrc = table.Column<string>(nullable: true),
                    AudioSrcUrl = table.Column<string>(nullable: true),
                    IsDefault = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNarrationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNarrationProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessionFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ContentDisposition = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    FileName = table.Column<string>(nullable: true),
                    Length = table.Column<long>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    FilePath = table.Column<string>(nullable: true),
                    MP3FileCheckSum = table.Column<string>(nullable: true),
                    ProcessResult = table.Column<bool>(nullable: false),
                    ProcessResultMsg = table.Column<string>(nullable: true),
                    UploadSessionId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessionFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSessionFile_UploadSessions_UploadSessionId",
                        column: x => x.UploadSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessionFile_UploadSessionId",
                table: "UploadSessionFile",
                column: "UploadSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_UserId",
                table: "UploadSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNarrationProfiles_UserId",
                table: "UserNarrationProfiles",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadSessionFile");

            migrationBuilder.DropTable(
                name: "UserNarrationProfiles");

            migrationBuilder.DropTable(
                name: "UploadSessions");
        }
    }
}
