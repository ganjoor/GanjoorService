using RMuseum.Models.Bookmark;
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
        /// Verse Id
        /// </summary>
        public int? VerseId { get; set; }

        /// <summary>
        /// Verse
        /// </summary>
        public virtual GanjoorVerse Verse { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public RBookmarkType RBookmarkType { get; set; }

        /// <summary>
        /// rating
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// User Note
        /// </summary>
        public string Note { get; set; }
    }
}
