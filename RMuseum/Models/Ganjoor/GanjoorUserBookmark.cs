using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Bookmark
    /// </summary>
    public class GanjoorUserBookmark
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// Poem Id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// Poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// Verse Id, this is always null, because verses are removed and created on operations like editing and their Ids changed, so
        /// later I decided to rely on (PoemId + CoupletIndex) instead
        /// </summary>
        public int? VerseId { get; set; }

        /// <summary>
        /// Verse 2 Id, this is always null, because verses are removed and created on operations like editing and their Ids changed, so
        /// later I decided to rely on (PoemId + CoupletIndex) instead
        /// </summary>
        public int? Verse2Id { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// private note for bookmark
        /// </summary>
        public string PrivateNote { get; set; }
    }
}
