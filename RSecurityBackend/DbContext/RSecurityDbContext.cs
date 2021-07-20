using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RSecurityBackend.Models.Audit.Db;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Models.Notification;
using System;
using System.IO;

namespace RSecurityBackend.DbContext
{
    /// <summary>
    /// Security EF DbContext
    /// </summary>
    public class RSecurityDbContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// parameterless constructor
        /// </summary>
        public RSecurityDbContext(DbContextOptions options)
            : base(options)
        {

        }
        /// <summary>
        /// delete db
        /// </summary>
        public void DeleteDb()
        {
            Database.EnsureDeleted();
        }
        /// <summary>
        /// OnConfiguring
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                   .Build();

                optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); //https://stackoverflow.com/a/34013431/66657

            builder.Entity<RVerifyQueueItem>()
                .HasIndex(u => u.Secret)
                .IsUnique();

            builder.Entity<RGenericOption>()
                .HasIndex(o => o.Name)
                .IsUnique();
        }        

        /// <summary>
        /// Permissions
        /// </summary>
        public DbSet<RPermission> Permissions { get; set; }

        /// <summary>
        /// User Sessions
        /// </summary>
        public DbSet<RTemporaryUserSession> Sessions { get; set; }


        /// <summary>
        /// Signup Queue Items
        /// </summary>
        public DbSet<RVerifyQueueItem> VerifyQueueItems { get; set; }

        /// <summary>
        /// General Images
        /// </summary>
        public DbSet<RImage> GeneralImages { get; set; }

        /// <summary>
        /// Captcha Images
        /// </summary>
        public DbSet<RCaptchaImage> CaptchaImages { get; set; }

        /// <summary>
        /// Audit Logs Events
        /// </summary>
        public DbSet<REvent> AuditLogs { get; set; }

        /// <summary>
        /// long running jobs
        /// </summary>
        public DbSet<RLongRunningJobStatus> LongRunningJobs { get; set; }

        /// <summary>
        /// Notifications
        /// </summary>
        public DbSet<RUserNotification> Notifications { get; set; }

        /// <summary>
        /// Options
        /// </summary>
        public DbSet<RGenericOption> Options { get; set; }
    }
}
