using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    TagType = table.Column<int>(nullable: false),
                    FriendlyUrl = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    NameInEnglish = table.Column<string>(nullable: true),
                    PluralName = table.Column<string>(nullable: true),
                    PluralNameInEnglish = table.Column<string>(nullable: true),
                    GlobalValue = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerifyQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    QueueType = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false),
                    Secret = table.Column<string>(nullable: true),
                    ClientIPAddress = table.Column<string>(nullable: true),
                    ClientAppName = table.Column<string>(nullable: true),
                    Language = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifyQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<Guid>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SecurableItemShortName = table.Column<string>(nullable: true),
                    OperationShortName = table.Column<string>(nullable: true),
                    RAppRoleId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_AspNetRoles_RAppRoleId",
                        column: x => x.RAppRoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    JobType = table.Column<int>(nullable: false),
                    ResourceNumber = table.Column<string>(nullable: true),
                    FriendlyUrl = table.Column<string>(nullable: true),
                    SrcUrl = table.Column<string>(nullable: true),
                    QueueTime = table.Column<DateTime>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    ProgressPercent = table.Column<decimal>(nullable: false),
                    Exception = table.Column<string>(nullable: true),
                    ArtifactId = table.Column<Guid>(nullable: true),
                    SrcContent = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RArtifactMasterRecordId = table.Column<Guid>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    FriendlyUrl = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    NameInEnglish = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    DescriptionInEnglish = table.Column<string>(nullable: true),
                    CoverImageIndex = table.Column<int>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    OriginalFileName = table.Column<string>(nullable: true),
                    ContentType = table.Column<string>(nullable: true),
                    FileSizeInBytes = table.Column<long>(nullable: false),
                    ImageWidth = table.Column<int>(nullable: false),
                    ImageHeight = table.Column<int>(nullable: false),
                    FolderName = table.Column<string>(nullable: true),
                    StoredFileName = table.Column<string>(nullable: true),
                    DataTime = table.Column<DateTime>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    TitleInEnglish = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    DescriptionInEnglish = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: true),
                    Order = table.Column<int>(nullable: true),
                    NormalSizeImageStoredFileName = table.Column<string>(nullable: true),
                    ThumbnailImageStoredFileName = table.Column<string>(nullable: true),
                    NormalSizeImageWidth = table.Column<int>(nullable: true),
                    NormalSizeImageHeight = table.Column<int>(nullable: true),
                    ThumbnailImageWidth = table.Column<int>(nullable: true),
                    ThumbnailImageHeight = table.Column<int>(nullable: true),
                    SrcUrl = table.Column<string>(nullable: true),
                    LastModifiedMeta = table.Column<DateTime>(nullable: true),
                    RArtifactItemRecordId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralImages_Items_RArtifactItemRecordId",
                        column: x => x.RArtifactItemRecordId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    FriendlyUrl = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    NameInEnglish = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    DescriptionInEnglish = table.Column<string>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    CoverItemIndex = table.Column<int>(nullable: false),
                    CoverImageId = table.Column<Guid>(nullable: false),
                    ItemCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artifacts_GeneralImages_CoverImageId",
                        column: x => x.CoverImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    SureName = table.Column<string>(nullable: true),
                    CreateDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    RImageId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_GeneralImages_RImageId",
                        column: x => x.RImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaptchaImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RImageId = table.Column<Guid>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptchaImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptchaImages_GeneralImages_RImageId",
                        column: x => x.RImageId,
                        principalTable: "GeneralImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RTagId = table.Column<Guid>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    FriendlyUrl = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    ValueInEnglish = table.Column<string>(nullable: true),
                    ValueSupplement = table.Column<string>(nullable: true),
                    RArtifactItemRecordId = table.Column<Guid>(nullable: true),
                    RArtifactMasterRecordId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagValues_Items_RArtifactItemRecordId",
                        column: x => x.RArtifactItemRecordId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TagValues_Artifacts_RArtifactMasterRecordId",
                        column: x => x.RArtifactMasterRecordId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TagValues_Tags_RTagId",
                        column: x => x.RTagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RAppUserId = table.Column<Guid>(nullable: false),
                    ClientIPAddress = table.Column<string>(nullable: true),
                    ClientAppName = table.Column<string>(nullable: true),
                    Language = table.Column<string>(nullable: true),
                    LoginTime = table.Column<DateTime>(nullable: false),
                    LastRenewal = table.Column<DateTime>(nullable: false),
                    ValidUntil = table.Column<DateTime>(nullable: false),
                    Token = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_AspNetUsers_RAppUserId",
                        column: x => x.RAppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_CoverImageId",
                table: "Artifacts",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_FriendlyUrl",
                table: "Artifacts",
                column: "FriendlyUrl",
                unique: true,
                filter: "[FriendlyUrl] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RImageId",
                table: "AspNetUsers",
                column: "RImageId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptchaImages_RImageId",
                table: "CaptchaImages",
                column: "RImageId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralImages_RArtifactItemRecordId",
                table: "GeneralImages",
                column: "RArtifactItemRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ArtifactId",
                table: "ImportJobs",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_RArtifactMasterRecordId_FriendlyUrl",
                table: "Items",
                columns: new[] { "RArtifactMasterRecordId", "FriendlyUrl" },
                unique: true,
                filter: "[FriendlyUrl] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Items_RArtifactMasterRecordId_Order",
                table: "Items",
                columns: new[] { "RArtifactMasterRecordId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RAppRoleId",
                table: "Permissions",
                column: "RAppRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RAppUserId",
                table: "Sessions",
                column: "RAppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FriendlyUrl",
                table: "Tags",
                column: "FriendlyUrl");

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_FriendlyUrl",
                table: "TagValues",
                column: "FriendlyUrl");

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_RArtifactItemRecordId",
                table: "TagValues",
                column: "RArtifactItemRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_RArtifactMasterRecordId",
                table: "TagValues",
                column: "RArtifactMasterRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TagValues_RTagId",
                table: "TagValues",
                column: "RTagId");

            migrationBuilder.CreateIndex(
                name: "IX_VerifyQueueItems_Secret",
                table: "VerifyQueueItems",
                column: "Secret",
                unique: true,
                filter: "[Secret] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportJobs_Artifacts_ArtifactId",
                table: "ImportJobs",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Artifacts_RArtifactMasterRecordId",
                table: "Items",
                column: "RArtifactMasterRecordId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Artifacts_GeneralImages_CoverImageId",
                table: "Artifacts");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CaptchaImages");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "TagValues");

            migrationBuilder.DropTable(
                name: "VerifyQueueItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "GeneralImages");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Artifacts");
        }
    }
}
