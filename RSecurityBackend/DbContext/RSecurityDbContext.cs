using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RSecurityBackend.Models.Audit.Db;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Generic.Db;
using RSecurityBackend.Models.Image;
using System;

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
        public RSecurityDbContext()
            : base()
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
        /// 
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); //https://stackoverflow.com/a/34013431/66657

            builder.Entity<RVerifyQueueItem>()
                .HasIndex(u => u.Secret)
                .IsUnique();
        }        

        #region Auth        
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


        #endregion

        #region Audit

        /// <summary>
        /// Audit Logs Events
        /// </summary>
        public DbSet<REvent> AuditLogs { get; set; }
        #endregion

        #region
        /// <summary>
        /// long running jobs
        /// </summary>
        public DbSet<RLongRunningJobStatus> LongRunningJobs { get; set; }
        #endregion
    }
}
