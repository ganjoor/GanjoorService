﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class NewTajikFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TajikDescription",
                table: "GanjoorPoets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TajikNickName",
                table: "GanjoorPoets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TajikTitle",
                table: "GanjoorPoems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TajikDescription",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TajikTitle",
                table: "GanjoorCategories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TajikDescription",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "TajikNickName",
                table: "GanjoorPoets");

            migrationBuilder.DropColumn(
                name: "TajikTitle",
                table: "GanjoorPoems");

            migrationBuilder.DropColumn(
                name: "TajikDescription",
                table: "GanjoorCategories");

            migrationBuilder.DropColumn(
                name: "TajikTitle",
                table: "GanjoorCategories");
        }
    }
}
