using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class WebpLogModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebpConvertionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileSizeInByte = table.Column<long>(type: "bigint", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetFileSizeInByte = table.Column<long>(type: "bigint", nullable: false),
                    FinishTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebpConvertionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebpConvertionLogs_GeneralImages_PictureId",
                        column: x => x.PictureId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebpConvertionLogs_PictureId",
                table: "WebpConvertionLogs",
                column: "PictureId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebpConvertionLogs");
        }
    }
}
