using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorCommentAbuseReport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorReportedComments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GanjoorCommentId = table.Column<int>(nullable: false),
                    ReasonCode = table.Column<string>(nullable: true),
                    ReasonText = table.Column<string>(nullable: true),
                    ReportedById = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorReportedComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorReportedComments_GanjoorComments_GanjoorCommentId",
                        column: x => x.GanjoorCommentId,
                        principalTable: "GanjoorComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GanjoorReportedComments_AspNetUsers_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorReportedComments_GanjoorCommentId",
                table: "GanjoorReportedComments",
                column: "GanjoorCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorReportedComments_ReportedById",
                table: "GanjoorReportedComments",
                column: "ReportedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorReportedComments");
        }
    }
}
