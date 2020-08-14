using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Auth.Db;
using System;


namespace RMuseum.Models.Bookmark
{
    /// <summary>
    /// User Bookmarks
    /// </summary>
    public class RUserBookmark
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid RAppUserId { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public RAppUser RAppUser { get; set; }

        /// <summary>
        /// Master Record Id
        /// </summary>
        public Guid? RArtifactMasterRecordId { get; set; }

        /// <summary>
        /// Master Record
        /// </summary>
        public virtual RArtifactMasterRecord RArtifactMasterRecord { get; set; }

        /// <summary>
        /// Item Record Id
        /// </summary>
        public Guid? RArtifactItemRecordId { get; set; }

        /// <summary>
        /// Item Record
        /// </summary>
        public virtual RArtifactItemRecord RArtifactItemRecord { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public RBookmarkType RBookmarkType { get; set; }

        /// <summary>
        /// User Note
        /// </summary>
        public string Note { get; set; }
    }
}
