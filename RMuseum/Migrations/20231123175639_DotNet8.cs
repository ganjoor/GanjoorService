using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class DotNet8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "GeneralImages",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "RWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RWSRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSRoles_RWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RWSUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RAppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InviteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MemberFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkspaceOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSUsers_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RWSUsers_RWorkspaces_RWorkspaceId",
                        column: x => x.RWorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceUserInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceUserInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceUserInvitations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceUserInvitations_RWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RWSPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurableItemShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperationShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RWSRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSPermissions_RWSRoles_RWSRoleId",
                        column: x => x.RWSRoleId,
                        principalTable: "RWSRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RWSUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RWSUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RWSUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RWSUserRoles_RWSRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RWSRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RWSUserRoles_RWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "RWorkspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RWSPermissions_RWSRoleId",
                table: "RWSPermissions",
                column: "RWSRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSRoles_WorkspaceId",
                table: "RWSRoles",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUserRoles_RoleId",
                table: "RWSUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUserRoles_UserId",
                table: "RWSUserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUserRoles_WorkspaceId",
                table: "RWSUserRoles",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUsers_RAppUserId",
                table: "RWSUsers",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RWSUsers_RWorkspaceId",
                table: "RWSUsers",
                column: "RWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceUserInvitations_UserId",
                table: "WorkspaceUserInvitations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceUserInvitations_WorkspaceId",
                table: "WorkspaceUserInvitations",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RWSPermissions");

            migrationBuilder.DropTable(
                name: "RWSUserRoles");

            migrationBuilder.DropTable(
                name: "RWSUsers");

            migrationBuilder.DropTable(
                name: "WorkspaceUserInvitations");

            migrationBuilder.DropTable(
                name: "RWSRoles");

            migrationBuilder.DropTable(
                name: "RWorkspaces");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "GeneralImages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(13)",
                oldMaxLength: 13);
        }
    }
}
