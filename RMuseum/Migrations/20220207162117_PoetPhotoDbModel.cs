using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class PoetPhotoDbModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoetSuggestedPictures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoetId = table.Column<int>(type: "int", nullable: false),
                    PicOrder = table.Column<int>(type: "int", nullable: false),
                    PictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    ChosenOne = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoetSuggestedPictures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoetSuggestedPictures_AspNetUsers_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoetSuggestedPictures_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorPoetSuggestedPictures_GeneralImages_PictureId",
                        column: x => x.PictureId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoetSuggestedPictures_PictureId",
                table: "GanjoorPoetSuggestedPictures",
                column: "PictureId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoetSuggestedPictures_PoetId",
                table: "GanjoorPoetSuggestedPictures",
                column: "PoetId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoetSuggestedPictures_SuggestedById",
                table: "GanjoorPoetSuggestedPictures",
                column: "SuggestedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoetSuggestedPictures");
        }
    }
}
