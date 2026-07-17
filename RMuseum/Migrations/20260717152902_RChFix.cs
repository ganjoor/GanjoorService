using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class RChFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RAppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EntityJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityUId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RChangeLogs_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RChangeLogs_RWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserOldEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOldEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOldEmails_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RChangeLogs_RAppUserId",
                table: "RChangeLogs",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RChangeLogs_WorkspaceId",
                table: "RChangeLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOldEmails_UserId",
                table: "UserOldEmails",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RChangeLogs");

            migrationBuilder.DropTable(
                name: "UserOldEmails");
        }
    }
}
