using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class SiteBanner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorSiteBanners",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RImageId = table.Column<Guid>(nullable: false),
                    AlternateText = table.Column<string>(nullable: true),
                    TargetUrl = table.Column<string>(nullable: true),
                    Active = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorSiteBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorSiteBanners_GeneralImages_RImageId",
                        column: x => x.RImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorSiteBanners_RImageId",
                table: "GanjoorSiteBanners",
                column: "RImageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorSiteBanners");
        }
    }
}
