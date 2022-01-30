using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    public partial class PoetSpecsModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPoetSuggestedSpecLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoetId = table.Column<int>(type: "int", nullable: false),
                    LineOrder = table.Column<int>(type: "int", nullable: false),
                    Contents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    SuggestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoetSuggestedSpecLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoetSuggestedSpecLines_AspNetUsers_SuggestedById",
                        column: x => x.SuggestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoetSuggestedSpecLines_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoetSuggestedSpecLines_PoetId",
                table: "GanjoorPoetSuggestedSpecLines",
                column: "PoetId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoetSuggestedSpecLines_SuggestedById",
                table: "GanjoorPoetSuggestedSpecLines",
                column: "SuggestedById");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoetSuggestedSpecLines");
        }
    }
}
