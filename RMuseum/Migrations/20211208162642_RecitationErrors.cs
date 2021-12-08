using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class RecitationErrors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecitationErrorReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecitationId = table.Column<int>(type: "int", nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecitationErrorReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecitationErrorReports_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecitationErrorReports_Recitations_RecitationId",
                        column: x => x.RecitationId,
                        principalTable: "Recitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecitationErrorReports_RecitationId",
                table: "RecitationErrorReports",
                column: "RecitationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecitationErrorReports_ReporterId",
                table: "RecitationErrorReports",
                column: "ReporterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecitationErrorReports");
        }
    }
}
