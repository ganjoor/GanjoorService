using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RMuseum.Migrations
{
    /// <inheritdoc />
    public partial class PDFLibEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "TagValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PDFBookId",
                table: "TagValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PDFPageId",
                table: "TagValues",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NameInOriginalLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtenalImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authors_GeneralImages_ImageId",
                        column: x => x.ImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtenalCoverImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_GeneralImages_CoverImageId",
                        column: x => x.CoverImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MultiVolumePDFCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiVolumePDFCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiVolumePDFCollections_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PDFBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorsLine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsTranslation = table.Column<bool>(type: "bit", nullable: false),
                    TranslatorsLine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleInOriginalLanguage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublisherLine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishingDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishingLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishingNumber = table.Column<int>(type: "int", nullable: true),
                    ClaimedPageCount = table.Column<int>(type: "int", nullable: true),
                    MultiVolumePDFCollectionId = table.Column<int>(type: "int", nullable: true),
                    VolumeOrder = table.Column<int>(type: "int", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PDFFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalPDFFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtenalCoverImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalSourceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalSourceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PDFBooks_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PDFBooks_GeneralImages_CoverImageId",
                        column: x => x.CoverImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PDFBooks_GeneralImages_PDFFileId",
                        column: x => x.PDFFileId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PDFBooks_MultiVolumePDFCollections_MultiVolumePDFCollectionId",
                        column: x => x.MultiVolumePDFCollectionId,
                        principalTable: "MultiVolumePDFCollections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuthorRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookId = table.Column<int>(type: "int", nullable: true),
                    PDFBookId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorRole_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuthorRole_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuthorRole_PDFBooks_PDFBookId",
                        column: x => x.PDFBookId,
                        principalTable: "PDFBooks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PDFPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PDFBookId = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    ThumbnailImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtenalThumbnailImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PDFPages_GeneralImages_ThumbnailImageId",
                        column: x => x.ThumbnailImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PDFPages_PDFBooks_PDFBookId",
                        column: x => x.PDFBookId,
                        principalTable: "PDFBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_BookId",
                table: "TagValues",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_PDFBookId",
                table: "TagValues",
                column: "PDFBookId");

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_PDFPageId",
                table: "TagValues",
                column: "PDFPageId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRole_AuthorId",
                table: "AuthorRole",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRole_BookId",
                table: "AuthorRole",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRole_PDFBookId",
                table: "AuthorRole",
                column: "PDFBookId");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_ImageId",
                table: "Authors",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CoverImageId",
                table: "Books",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiVolumePDFCollections_BookId",
                table: "MultiVolumePDFCollections",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFBooks_BookId",
                table: "PDFBooks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFBooks_CoverImageId",
                table: "PDFBooks",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFBooks_MultiVolumePDFCollectionId",
                table: "PDFBooks",
                column: "MultiVolumePDFCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFBooks_PDFFileId",
                table: "PDFBooks",
                column: "PDFFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFPages_PDFBookId",
                table: "PDFPages",
                column: "PDFBookId");

            migrationBuilder.CreateIndex(
                name: "IX_PDFPages_ThumbnailImageId",
                table: "PDFPages",
                column: "ThumbnailImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_TagValues_Books_BookId",
                table: "TagValues",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagValues_PDFBooks_PDFBookId",
                table: "TagValues",
                column: "PDFBookId",
                principalTable: "PDFBooks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagValues_PDFPages_PDFPageId",
                table: "TagValues",
                column: "PDFPageId",
                principalTable: "PDFPages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagValues_Books_BookId",
                table: "TagValues");

            migrationBuilder.DropForeignKey(
                name: "FK_TagValues_PDFBooks_PDFBookId",
                table: "TagValues");

            migrationBuilder.DropForeignKey(
                name: "FK_TagValues_PDFPages_PDFPageId",
                table: "TagValues");

            migrationBuilder.DropTable(
                name: "AuthorRole");

            migrationBuilder.DropTable(
                name: "PDFPages");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "PDFBooks");

            migrationBuilder.DropTable(
                name: "MultiVolumePDFCollections");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropIndex(
                name: "IX_TagValues_BookId",
                table: "TagValues");

            migrationBuilder.DropIndex(
                name: "IX_TagValues_PDFBookId",
                table: "TagValues");

            migrationBuilder.DropIndex(
                name: "IX_TagValues_PDFPageId",
                table: "TagValues");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "TagValues");

            migrationBuilder.DropColumn(
                name: "PDFBookId",
                table: "TagValues");

            migrationBuilder.DropColumn(
                name: "PDFPageId",
                table: "TagValues");
        }
    }
}
