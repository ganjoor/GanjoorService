using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class WorkspaceGenericOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Options",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Options_WorkspaceId",
                table: "Options",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Options_RWorkspaces_WorkspaceId",
                table: "Options",
                column: "WorkspaceId",
                principalTable: "RWorkspaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Options_RWorkspaces_WorkspaceId",
                table: "Options");

            migrationBuilder.DropIndex(
                name: "IX_Options_WorkspaceId",
                table: "Options");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Options");
        }
    }
}
