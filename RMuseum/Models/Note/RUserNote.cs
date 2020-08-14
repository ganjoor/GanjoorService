using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Note
{
    /// <summary>
    /// User Note
    /// </summary>
    public class RUserNote
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
        /// Is Updated by User
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// Last Modified
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Private / Public
        /// </summary>
        public RNoteType NoteType { get; set; }

        /// <summary>
        /// content
        /// </summary>
        public string HtmlContent { get; set; }

        /// <summary>
        /// In Reply to Other Note
        /// </summary>
        public virtual RUserNote ReferenceNote { get; set; }

        /// <summary>
        /// Reference Note Id
        /// </summary>
        public Guid? ReferenceNoteId { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public PublishStatus Status { get; set; }
    }
}
