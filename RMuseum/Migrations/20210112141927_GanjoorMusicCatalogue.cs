using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorMusicCatalogue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorSingers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorSingers", x => x.Id);
                });

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
                        name: "FK_GanjoorAlbum_GanjoorSingers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "GanjoorSingers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "GanjoorMusicCatalogueTracks",
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
                    table.PrimaryKey("PK_GanjoorMusicCatalogueTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorMusicCatalogueTracks_GanjoorAlbum_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "GanjoorAlbum",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GolhaTracks",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
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
                    table.PrimaryKey("PK_GolhaTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GolhaTracks_GolhaProgram_GolhaProgramId",
                        column: x => x.GolhaProgramId,
                        principalTable: "GolhaProgram",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GolhaTracks_GanjoorSingers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "GanjoorSingers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorPoemMusicTracks",
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
                    table.PrimaryKey("PK_GanjoorPoemMusicTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemMusicTracks_GanjoorMusicCatalogueTracks_GanjoorTrackId",
                        column: x => x.GanjoorTrackId,
                        principalTable: "GanjoorMusicCatalogueTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemMusicTracks_GolhaTracks_GolhaTrackId",
                        column: x => x.GolhaTrackId,
                        principalTable: "GolhaTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemMusicTracks_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemMusicTracks_GanjoorSingers_SingerId",
                        column: x => x.SingerId,
                        principalTable: "GanjoorSingers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemMusicTracks_AspNetUsers_SuggestedById",
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
                name: "IX_GanjoorMusicCatalogueTracks_AlbumId",
                table: "GanjoorMusicCatalogueTracks",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorMusicCatalogueTracks_Name",
                table: "GanjoorMusicCatalogueTracks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemMusicTracks_GanjoorTrackId",
                table: "GanjoorPoemMusicTracks",
                column: "GanjoorTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemMusicTracks_GolhaTrackId",
                table: "GanjoorPoemMusicTracks",
                column: "GolhaTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemMusicTracks_PoemId",
                table: "GanjoorPoemMusicTracks",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemMusicTracks_SingerId",
                table: "GanjoorPoemMusicTracks",
                column: "SingerId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemMusicTracks_SuggestedById",
                table: "GanjoorPoemMusicTracks",
                column: "SuggestedById");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorSingers_Name",
                table: "GanjoorSingers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GolhaProgram_GolhaCollectionId",
                table: "GolhaProgram",
                column: "GolhaCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GolhaTracks_GolhaProgramId",
                table: "GolhaTracks",
                column: "GolhaProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_GolhaTracks_SingerId",
                table: "GolhaTracks",
                column: "SingerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoemMusicTracks");

            migrationBuilder.DropTable(
                name: "GanjoorMusicCatalogueTracks");

            migrationBuilder.DropTable(
                name: "GolhaTracks");

            migrationBuilder.DropTable(
                name: "GanjoorAlbum");

            migrationBuilder.DropTable(
                name: "GolhaProgram");

            migrationBuilder.DropTable(
                name: "GanjoorSingers");

            migrationBuilder.DropTable(
                name: "GolhaCollections");
        }
    }
}
