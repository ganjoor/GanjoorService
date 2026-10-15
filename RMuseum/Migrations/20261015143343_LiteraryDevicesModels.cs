using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class LiteraryDevicesModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LiteraryAnnotationCompleted",
                table: "GanjoorPoemCorrections",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LiteraryConcepts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Aliases = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiteraryConcepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiteraryDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Example = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCore = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SuggestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiteraryDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoemLiteraryAnnotationStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemLiteraryAnnotationStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GanjoorPoemLiteraryTagCorrections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Uncertain = table.Column<bool>(type: "bit", nullable: false),
                    PartsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarkForDelete = table.Column<bool>(type: "bit", nullable: false),
                    ExistingTagId = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GanjoorPoemCorrectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GanjoorPoemLiteraryTagCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GanjoorPoemLiteraryTagCorrections_GanjoorPoemCorrections_GanjoorPoemCorrectionId",
                        column: x => x.GanjoorPoemCorrectionId,
                        principalTable: "GanjoorPoemCorrections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GanjoorPoemLiteraryTagCorrections_LiteraryDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "LiteraryDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LiteraryDeviceSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    AllowMultiple = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiteraryDeviceSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiteraryDeviceSlots_LiteraryDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "LiteraryDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoemLiteraryTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoemId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Uncertain = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    OriginRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemLiteraryTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoemLiteraryTags_GanjoorPoems_PoemId",
                        column: x => x.PoemId,
                        principalTable: "GanjoorPoems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoemLiteraryTags_LiteraryDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "LiteraryDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PoemLiteraryTagParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    CoupletIndex = table.Column<int>(type: "int", nullable: false),
                    Misra = table.Column<int>(type: "int", nullable: false),
                    WordStart = table.Column<int>(type: "int", nullable: true),
                    WordEnd = table.Column<int>(type: "int", nullable: true),
                    ConceptId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoemLiteraryTagParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoemLiteraryTagParts_LiteraryConcepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "LiteraryConcepts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PoemLiteraryTagParts_LiteraryDeviceSlots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "LiteraryDeviceSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoemLiteraryTagParts_PoemLiteraryTags_TagId",
                        column: x => x.TagId,
                        principalTable: "PoemLiteraryTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemLiteraryTagCorrections_DeviceId",
                table: "GanjoorPoemLiteraryTagCorrections",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemLiteraryTagCorrections_GanjoorPoemCorrectionId",
                table: "GanjoorPoemLiteraryTagCorrections",
                column: "GanjoorPoemCorrectionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiteraryConcepts_Name",
                table: "LiteraryConcepts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LiteraryConcepts_ParentId",
                table: "LiteraryConcepts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_LiteraryDevices_Name",
                table: "LiteraryDevices",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LiteraryDeviceSlots_DeviceId",
                table: "LiteraryDeviceSlots",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemLiteraryAnnotationStatuses_PoemId",
                table: "PoemLiteraryAnnotationStatuses",
                column: "PoemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoemLiteraryTagParts_ConceptId",
                table: "PoemLiteraryTagParts",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemLiteraryTagParts_SlotId",
                table: "PoemLiteraryTagParts",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemLiteraryTagParts_TagId",
                table: "PoemLiteraryTagParts",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemLiteraryTags_DeviceId",
                table: "PoemLiteraryTags",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PoemLiteraryTags_PoemId_CoupletIndex",
                table: "PoemLiteraryTags",
                columns: new[] { "PoemId", "CoupletIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GanjoorPoemLiteraryTagCorrections");

            migrationBuilder.DropTable(
                name: "PoemLiteraryAnnotationStatuses");

            migrationBuilder.DropTable(
                name: "PoemLiteraryTagParts");

            migrationBuilder.DropTable(
                name: "LiteraryConcepts");

            migrationBuilder.DropTable(
                name: "LiteraryDeviceSlots");

            migrationBuilder.DropTable(
                name: "PoemLiteraryTags");

            migrationBuilder.DropTable(
                name: "LiteraryDevices");

            migrationBuilder.DropColumn(
                name: "LiteraryAnnotationCompleted",
                table: "GanjoorPoemCorrections");
        }
    }
}
