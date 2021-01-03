using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class GanjoorPages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GanjoorPages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    GanjoorPageType = table.Column<int>(nullable: false),
                    Published = table.Column<bool>(nullable: false),
                    PageOrder = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    FullTitle = table.Column<string>(nullable: true),
                    UrlSlug = table.Column<string>(nullable: true),
                    FullUrl = table.Column<string>(nullable: true),
                    HtmlText = table.Column<string>(nullable: true),
                    ParentId = table.Column<int>(nullable: true),
                    PoetId = table.Column<int>(nullable: true),
                    CatId = table.Column<int>(nullable: true),
                    PoemId = table.Column<int>(nullable: true),
                    SecondPoetId = table.Column<int>(nullable: true),
                    PostDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPages_GanjoorCategories_CatId",
                        column: x => x.CatId,
                        principalTable: "GanjoorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPages_GanjoorPages_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GanjoorPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPages_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPages_GanjoorPoets_PoetId",
                        column: x => x.PoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GanjoorPages_GanjoorPoets_SecondPoetId",
                        column: x => x.SecondPoetId,
                        principalTable: "GanjoorPoets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPages_CatId",
                table: "GanjoorPages",
                column: "CatId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPages_ParentId",
                table: "GanjoorPages",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPages_PoemId",
                table: "GanjoorPages",
                column: "PoemId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPages_PoetId",
                table: "GanjoorPages",
                column: "PoetId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPages_SecondPoetId",
                table: "GanjoorPages",
                column: "SecondPoetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPages");
        }
    }
}
