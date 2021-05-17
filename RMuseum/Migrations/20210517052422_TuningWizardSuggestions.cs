using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RMuseum.Migrations
{
    public partial class TuningWizardSuggestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GanjoorCategories_ParentId",
                table: "GanjoorCategories");

            migrationBuilder.CreateIndex(
                name: "IX_Recitations_GanjoorAudioId",
                table: "Recitations",
                column: "GanjoorAudioId");

            migrationBuilder.CreateIndex(
                name: "IX_Recitations_ReviewStatus_GanjoorPostId",
                table: "Recitations",
                columns: new[] { "ReviewStatus", "GanjoorPostId" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoets_Published_Id",
                table: "GanjoorPoets",
                columns: new[] { "Published", "Id" })
                .Annotation("SqlServer:Include", new[] { "Name", "Nickname", "RImageId" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoems_Id",
                table: "GanjoorPoems",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorPoemMusicTracks_Approved_Rejected",
                table: "GanjoorPoemMusicTracks",
                columns: new[] { "Approved", "Rejected" });

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorComments_Status",
                table: "GanjoorComments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_ParentId_PoetId",
                table: "GanjoorCategories",
                columns: new[] { "ParentId", "PoetId" })
                .Annotation("SqlServer:Include", new[] { "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_LastModified",
                table: "Artifacts",
                column: "LastModified");

            migrationBuilder.Sql("CREATE STATISTICS [_ST_Recitations_GanjoorPostIdReviewStatus] ON [dbo].[Recitations]([GanjoorPostId], [ReviewStatus])");
            migrationBuilder.Sql("CREATE STATISTICS [_ST_GanjoorCategories_PoetIdParentId] ON [dbo].[GanjoorCategories]([PoetId], [ParentId])");
            migrationBuilder.Sql("CREATE STATISTICS [_ST_Artifacts_CoverItemIndexStatus] ON [dbo].[Artifacts]([CoverItemIndex], [Status])");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recitations_GanjoorAudioId",
                table: "Recitations");

            migrationBuilder.DropIndex(
                name: "IX_Recitations_ReviewStatus_GanjoorPostId",
                table: "Recitations");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoets_Published_Id",
                table: "GanjoorPoets");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoems_Id",
                table: "GanjoorPoems");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorPoemMusicTracks_Approved_Rejected",
                table: "GanjoorPoemMusicTracks");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorComments_Status",
                table: "GanjoorComments");

            migrationBuilder.DropIndex(
                name: "IX_GanjoorCategories_ParentId_PoetId",
                table: "GanjoorCategories");

            migrationBuilder.DropIndex(
                name: "IX_Artifacts_LastModified",
                table: "Artifacts");

            migrationBuilder.CreateIndex(
                name: "IX_GanjoorCategories_ParentId",
                table: "GanjoorCategories",
                column: "ParentId");

            migrationBuilder.Sql("DROP STATISTICS Recitations._ST_Recitations_GanjoorPostIdReviewStatus");
            migrationBuilder.Sql("DROP STATISTICS GanjoorCategories._ST_GanjoorCategories_PoetIdParentId");
            migrationBuilder.Sql("DROP STATISTICS Artifacts._ST_Artifacts_CoverItemIndexStatus");
        }
    }
}
