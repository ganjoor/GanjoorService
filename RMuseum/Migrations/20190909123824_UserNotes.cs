using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class UserNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RAppUserId = table.Column<Guid>(nullable: false),
                    RArtifactMasterRecordId = table.Column<Guid>(nullable: true),
                    RArtifactItemRecordId = table.Column<Guid>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false),
                    Modified = table.Column<bool>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    NoteType = table.Column<int>(nullable: false),
                    HtmlContent = table.Column<string>(nullable: true),
                    ReferenceNoteId = table.Column<Guid>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotes_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotes_Items_RArtifactItemRecordId",
                        column: x => x.RArtifactItemRecordId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserNotes_Artifacts_RArtifactMasterRecordId",
                        column: x => x.RArtifactMasterRecordId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserNotes_UserNotes_ReferenceNoteId",
                        column: x => x.ReferenceNoteId,
                        principalTable: "UserNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_RAppUserId",
                table: "UserNotes",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_RArtifactItemRecordId",
                table: "UserNotes",
                column: "RArtifactItemRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_RArtifactMasterRecordId",
                table: "UserNotes",
                column: "RArtifactMasterRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_ReferenceNoteId",
                table: "UserNotes",
                column: "ReferenceNoteId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotes");
        }
    }
}
