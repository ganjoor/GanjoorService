using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorMusicCatalogue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GolhaCollections",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolhaCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Singers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Singers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GolhaProgram",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    GolhaCollectionId = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    ProgramOrder = table.Column<int>(nullable: false),
                    Url = table.Column<string>(nullable: true),
                    Mp3 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolhaProgram", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GolhaProgram_GolhaCollections_GolhaCollectionId",
                        column: x => x.GolhaCollectionId,
                        principalTable: "GolhaCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorAlbum",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SingerId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorAlbum", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorAlbum_Singers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "Singers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GolhaTrack",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GolhaProgramId = table.Column<int>(nullable: false),
                    TrackNo = table.Column<int>(nullable: false),
                    Timing = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    SingerId = table.Column<int>(nullable: true),
                    Blocked = table.Column<bool>(nullable: false),
                    BlockReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GolhaTrack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GolhaTrack_GolhaProgram_GolhaProgramId",
                        column: x => x.GolhaProgramId,
                        principalTable: "GolhaProgram",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GolhaTrack_Singers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "Singers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MusicTracks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true),
                    Blocked = table.Column<bool>(nullable: false),
                    BlockReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicTracks_GanjoorAlbum_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "GanjoorAlbum",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoemMusicTracks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(nullable: false),
                    TrackType = table.Column<int>(nullable: false),
                    ArtistName = table.Column<string>(nullable: true),
                    ArtistUrl = table.Column<string>(nullable: true),
                    AlbumName = table.Column<string>(nullable: true),
                    AlbumUrl = table.Column<string>(nullable: true),
                    TrackName = table.Column<string>(nullable: true),
                    TrackUrl = table.Column<string>(nullable: true),
                    GanjoorTrackId = table.Column<int>(nullable: true),
                    SingerId = table.Column<int>(nullable: true),
                    GolhaTrackId = table.Column<int>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Approved = table.Column<bool>(nullable: false),
                    SuggestedById = table.Column<Guid>(nullable: true),
                    ApprovalDate = table.Column<DateTime>(nullable: false),
                    BrokenLink = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemMusicTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoemMusicTracks_MusicTracks_GanjoorTrackId",
                        column: x => x.GanjoorTrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoemMusicTracks_GolhaTrack_GolhaTrackId",
                        column: x => x.GolhaTrackId,
                        principalTable: "GolhaTrack",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoemMusicTracks_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoemMusicTracks_Singers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "Singers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoemMusicTracks_AspNetUsers_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorAlbum_SingerId",
                table: "GanjoorAlbum",
                column: "SingerId");

            migrationBuilder.CreateIndex(
                name: "IX_GolhaProgram_GolhaCollectionId",
                table: "GolhaProgram",
                column: "GolhaCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GolhaTrack_GolhaProgramId",
                table: "GolhaTrack",
                column: "GolhaProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_GolhaTrack_SingerId",
                table: "GolhaTrack",
                column: "SingerId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_AlbumId",
                table: "MusicTracks",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_Name",
                table: "MusicTracks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PoemMusicTracks_GanjoorTrackId",
                table: "PoemMusicTracks",
                column: "GanjoorTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemMusicTracks_GolhaTrackId",
                table: "PoemMusicTracks",
                column: "GolhaTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemMusicTracks_PoemId",
                table: "PoemMusicTracks",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemMusicTracks_SingerId",
                table: "PoemMusicTracks",
                column: "SingerId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemMusicTracks_SuggestedById",
                table: "PoemMusicTracks",
                column: "SuggestedById");

            migrationBuilder.CreateIndex(
                name: "IX_Singers_Name",
                table: "Singers",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoemMusicTracks");

            migrationBuilder.DropTable(
                name: "MusicTracks");

            migrationBuilder.DropTable(
                name: "GolhaTrack");

            migrationBuilder.DropTable(
                name: "GanjoorAlbum");

            migrationBuilder.DropTable(
                name: "GolhaProgram");

            migrationBuilder.DropTable(
                name: "Singers");

            migrationBuilder.DropTable(
                name: "GolhaCollections");
        }
    }
}
