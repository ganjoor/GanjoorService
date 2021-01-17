using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
using Dapper;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.MusicCatalogue;

namespace RMuseum.DbContext
{
    /// <summary>
    /// Museum Database Context
    /// </summary>
    public class RMuseumDbContext : RSecurityDbContext<RAppUser, RAppRole, Guid>
    {
        /// <summary>
        /// constructor
        /// </summary>
        public RMuseumDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
            Database.Migrate();
        }

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            base.OnConfiguring(optionsBuilder);
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
        /// PoemMusicTracks
        /// </summary>
        public DbSet<PoemMusicTrack> GanjoorPoemMusicTracks { get; set; }
        

    }
}
