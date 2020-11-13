using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class NarrationPublishingTrackers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NarrationPublishingTrackers",
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
                    table.PrimaryKey("PK_NarrationPublishingTrackers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrationPublishingTrackers_AudioFiles_PoemNarrationId",
                        column: x => x.PoemNarrationId,
                        principalTable: "AudioFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NarrationPublishingTrackers_PoemNarrationId",
                table: "NarrationPublishingTrackers",
                column: "PoemNarrationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NarrationPublishingTrackers");
        }
    }
}
