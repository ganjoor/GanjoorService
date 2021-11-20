using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class ReportedUserNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportedUserNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportedUserNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportedUserNotes_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportedUserNotes_UserNotes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "UserNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportedUserNotes_NoteId",
                table: "ReportedUserNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportedUserNotes_ReporterId",
                table: "ReportedUserNotes",
                column: "ReporterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportedUserNotes");
        }
    }
}
