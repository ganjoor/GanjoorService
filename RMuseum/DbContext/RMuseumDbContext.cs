using Microsoft.EntityFrameworkCore;
using RMuseum.Models.Artifact;
using RMuseum.Models.Bookmark;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.UploadSession;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.Models.ImportJob;
using RMuseum.Models.Note;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using System;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.MusicCatalogue;
using RMuseum.Models.Accounting;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RMuseum.DbContext
{
    /// <summary>
    /// Museum Database Context
    /// </summary>
    public class RMuseumDbContext : RSecurityDbContext<RAppUser, RAppRole, Guid>
    {
        public RMuseumDbContext(DbContextOptions<RMuseumDbContext> options) : base(options)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                   .Build();
            if (bool.Parse(configuration["DatabaseMigrate"]))
            {
                Database.Migrate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RArtifactMasterRecord>()
                .HasIndex(m => m.FriendlyUrl)
                .IsUnique();

            builder.Entity<RArtifactItemRecord>()
                .HasIndex(i => new { i.RArtifactMasterRecordId, i.FriendlyUrl })
                .IsUnique();

            builder.Entity<RArtifactItemRecord>()
                .HasIndex(i => new { i.RArtifactMasterRecordId, i.Order })
                .IsUnique();

            builder.Entity<RTag>()
                .HasIndex(t => t.FriendlyUrl);

            builder.Entity<RTagValue>()
                .HasIndex(t => t.FriendlyUrl);

            builder.Entity<Recitation>()
                .HasIndex(p => p.GanjoorPostId);

            builder.Entity<GanjoorCat>()
                .HasIndex(c => c.FullUrl);

            builder.Entity<GanjoorPoem>()
                .HasIndex(c => c.FullUrl);

            builder.Entity<GanjoorPage>()
                 .HasIndex(c => c.FullUrl);

            builder.Entity<GanjoorSinger>()
                .HasIndex(c => c.Name);

            builder.Entity<GanjoorTrack>()
                .HasIndex(c => c.Name);

            builder.Entity<GanjoorComment>()
                .HasIndex(c => c.CommentDate);

            builder.Entity<ImportJob>()
               .Property(c => c.ProgressPercent)
               .HasColumnType("decimal(18,2)");

            //Index set suggested by SQL Server Tuning Wizard -- begin

            builder.Entity<GanjoorComment>()
                .HasIndex(c => c.Status);

            builder.Entity<GanjoorPoem>()
                .HasIndex(c => c.Id);

            builder.Entity<Recitation>()
                .HasIndex(c => c.GanjoorAudioId);

            builder.Entity<Recitation>()
                .HasIndex(c => new { c.ReviewStatus, c.GanjoorPostId });

            builder.Entity<RArtifactMasterRecord>()
                .HasIndex(c => c.LastModified);

            builder.Entity<GanjoorPoet>()
               .HasIndex(c => new { c.Published, c.Id })
               .IncludeProperties(c => new { c.Name, c.Nickname, c.RImageId });

            builder.Entity<GanjoorCat>()
                .HasIndex(c => new { c.ParentId, c.PoetId })
                .IncludeProperties(c => c.Id);

            builder.Entity<PoemMusicTrack>()
                .HasIndex(c => new { c.Approved, c.Rejected });

            builder.Entity<RArtifactMasterRecord>()
                .HasIndex(c => new { c.CoverItemIndex, c.Status });


            builder.Entity<GanjoorLanguage>()
                .HasIndex(m => m.Name)
                .IsUnique();

            builder.Entity<GanjoorUserBookmark>().HasOne(b => b.Verse)
                 .WithMany().Metadata.DeleteBehavior = DeleteBehavior.Restrict;

            builder.Entity<GanjoorUserBookmark>()
                .HasIndex(b => new { b.UserId, b.PoemId, b.VerseId })
                .IsUnique();

            builder.Entity<GanjoorVerseNumber>()
                .HasIndex(n => new { n.NumberingId, n.PoemId, n.CoupletIndex })
                .IsUnique();

            builder.Entity<GanjoorVerseNumber>()
                .HasIndex(n => new { n.PoemId, n.CoupletIndex });

            builder.Entity<GanjoorVerse>()
                .HasIndex(v => v.PoemId);//the next statement causes a drop index in the migration which this line prevents it

            builder.Entity<GanjoorVerse>()
                .HasIndex(v => new { v.PoemId, v.CoupletIndex });


            //Index set suggested by SQL Server Tuning Wizard -- end

            builder.Entity<GanjoorDonation>()
              .Property(c => c.Amount)
              .HasColumnType("decimal(18,2)");

            builder.Entity<GanjoorDonation>()
             .Property(c => c.Remaining)
             .HasColumnType("decimal(18,2)");

            builder.Entity<GanjoorExpense>()
             .Property(c => c.Amount)
             .HasColumnType("decimal(18,2)");

            builder.Entity<DonationExpenditure>()
            .Property(c => c.Amount)
            .HasColumnType("decimal(18,2)");
        }


        /// <summary>
        /// Picture Files
        /// </summary>
        public DbSet<RPictureFile> PictureFiles { get; set; }

        /// <summary>
        /// Item Attributes
        /// </summary>
        public DbSet<RTag> Tags { get; set; }

        /// <summary>
        /// Artifacts
        /// </summary>
        public DbSet<RArtifactMasterRecord> Artifacts { get; set; }

        /// <summary>
        /// Items
        /// </summary>
        public DbSet<RArtifactItemRecord> Items { get; set; }


        /// <summary>
        /// Import Jobs
        /// </summary>
        public DbSet<ImportJob> ImportJobs { get; set; }

        /// <summary>
        /// Tags
        /// </summary>
        public DbSet<RTagValue> TagValues { get; set; }

        /// <summary>
        /// User Bookmarks
        /// </summary>
        public DbSet<RUserBookmark> UserBookmarks { get; set; }

        /// <summary>
        /// User Notes
        /// </summary>
        public DbSet<RUserNote> UserNotes { get; set; }

        /// <summary>
        /// Ganjoor Links
        /// </summary>
        public DbSet<GanjoorLink> GanjoorLinks { get; set; }


        /// <summary>
        /// Pinterest Links
        /// </summary>
        public DbSet<PinterestLink> PinterestLinks { get; set; }

        /// <summary>
        /// Ganjoor Audio Files
        /// </summary>
        public DbSet<Recitation> Recitations { get; set; }

        /// <summary>
        /// Upload Sessions
        /// </summary>
        public DbSet<UploadSession> UploadSessions { get; set; }

        /// <summary>
        /// Uploaded files
        /// </summary>
        public DbSet<UploadSessionFile> UploadedFiles { get; set; }

        /// <summary>
        /// User Recitation Profiles
        /// </summary>
        public DbSet<UserRecitationProfile> UserRecitationProfiles { get; set; }

        /// <summary>
        /// Ganjoor Poets
        /// </summary>
        public DbSet<GanjoorPoet> GanjoorPoets { get; set; }

        /// <summary>
        /// Ganjoor Categories
        /// </summary>
        public DbSet<GanjoorCat> GanjoorCategories { get; set; }

        /// <summary>
        /// Ganjoor Poems
        /// </summary>
        public DbSet<GanjoorPoem> GanjoorPoems { get; set; }

        /// <summary>
        /// Ganjoor Verses
        /// </summary>
        public DbSet<GanjoorVerse> GanjoorVerses { get; set; }

        /// <summary>
        /// Narration Publishing Tracker
        /// </summary>
        public DbSet<RecitationPublishingTracker> RecitationPublishingTrackers { get; set; }

        /// <summary>
        /// Ganjoor Pages
        /// </summary>
        public DbSet<GanjoorPage> GanjoorPages { get; set; }

        /// <summary>
        /// Ganjoor Metres
        /// </summary>
        public DbSet<GanjoorMetre> GanjoorMetres { get; set; }


        /// <summary>
        /// singers
        /// </summary>
        public DbSet<GanjoorSinger> GanjoorSingers { get; set; }

        /// <summary>
        /// music tracks
        /// </summary>
        public DbSet<GanjoorTrack> GanjoorMusicCatalogueTracks { get; set; }

        /// <summary>
        /// golha tracks
        /// </summary>
        public DbSet<GolhaTrack> GolhaTracks { get; set; }

        /// <summary>
        /// GolhaCollection 
        /// </summary>
        public DbSet<GolhaCollection> GolhaCollections { get; set; }

        /// <summary>
        /// GolhaPrograms 
        /// </summary>
        public DbSet<GolhaProgram> GolhaPrograms { get; set; }

        /// <summary>
        /// PoemMusicTracks
        /// </summary>
        public DbSet<PoemMusicTrack> GanjoorPoemMusicTracks { get; set; }

        /// <summary>
        /// Ganjoor Comments
        /// </summary>
        public DbSet<GanjoorComment> GanjoorComments { get; set; }

        /// <summary>
        /// Ganjoor Reported Comments
        /// </summary>
        public DbSet<GanjoorCommentAbuseReport> GanjoorReportedComments { get; set; }

        /// <summary>
        /// Ganjoor Page Snapshots
        /// </summary>
        public DbSet<GanjoorPageSnapshot> GanjoorPageSnapshots { get; set; }


        /// <summary>
        /// Ganjoor Site Bannaers
        /// </summary>
        public DbSet<GanjoorSiteBanner> GanjoorSiteBanners { get; set; }

        /// <summary>
        /// Ganjoor Health Check Errors
        /// </summary>
        public DbSet<GanjoorHealthCheckError> GanjoorHealthCheckErrors { get; set; }

        /// <summary>
        /// donations
        /// </summary>
        public DbSet<GanjoorDonation> GanjoorDonations { get; set; }

        /// <summary>
        /// expenses
        /// </summary>
        public DbSet<GanjoorExpense> GanjoorExpenses { get; set; }

        /// <summary>
        /// donation expenditures
        /// </summary>
        public DbSet<DonationExpenditure> DonationExpenditure { get; set; }

        /// <summary>
        /// poem corrections
        /// </summary>
        public DbSet<GanjoorPoemCorrection> GanjoorPoemCorrections { get; set; }

        /// <summary>
        /// languages for translation
        /// </summary>
        public DbSet<GanjoorLanguage> GanjoorLanguages { get; set; }

        /// <summary>
        /// poem translations
        /// </summary>
        public DbSet<GanjoorPoemTranslation> GanjoorPoemTranslations { get; set; }

        /// <summary>
        /// ganjoor bookmarks
        /// </summary>
        public DbSet<GanjoorUserBookmark> GanjoorUserBookmarks { get; set; }

        /// <summary>
        /// ganjoor numbering schemas
        /// </summary>
        public DbSet<GanjoorNumbering> GanjoorNumberings { get; set; }

        /// <summary>
        /// ganjoor verse numbers
        /// </summary>
        public DbSet<GanjoorVerseNumber> GanjoorVerseNumbers { get; set; }

    }
}
