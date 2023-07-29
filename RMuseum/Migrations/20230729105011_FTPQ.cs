using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class FTPQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueuedFTPUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemoteFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteFileAfterUpload = table.Column<bool>(type: "bit", nullable: false),
                    QueueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Processing = table.Column<bool>(type: "bit", nullable: false),
                    ProcessDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedFTPUploads", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueuedFTPUploads");
        }
    }
}
