using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class UploadedFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadSessionFile_UploadSessions_UploadSessionId",
                table: "UploadSessionFile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UploadSessionFile",
                table: "UploadSessionFile");

            migrationBuilder.RenameTable(
                name: "UploadSessionFile",
                newName: "UploadedFiles");

            migrationBuilder.RenameIndex(
                name: "IX_UploadSessionFile_UploadSessionId",
                table: "UploadedFiles",
                newName: "IX_UploadedFiles_UploadSessionId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UploadSessionId",
                table: "UploadedFiles",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UploadedFiles",
                table: "UploadedFiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedFiles_UploadSessions_UploadSessionId",
                table: "UploadedFiles",
                column: "UploadSessionId",
                principalTable: "UploadSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadedFiles_UploadSessions_UploadSessionId",
                table: "UploadedFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UploadedFiles",
                table: "UploadedFiles");

            migrationBuilder.RenameTable(
                name: "UploadedFiles",
                newName: "UploadSessionFile");

            migrationBuilder.RenameIndex(
                name: "IX_UploadedFiles_UploadSessionId",
                table: "UploadSessionFile",
                newName: "IX_UploadSessionFile_UploadSessionId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UploadSessionId",
                table: "UploadSessionFile",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UploadSessionFile",
                table: "UploadSessionFile",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadSessionFile_UploadSessions_UploadSessionId",
                table: "UploadSessionFile",
                column: "UploadSessionId",
                principalTable: "UploadSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
