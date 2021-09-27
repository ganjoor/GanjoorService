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
        public int VerseId { get; set; }

        /// <summary>
        /// Verse
        /// </summary>
        public GanjoorVerse Verse { get; set; }

        /// <summary>
        /// Verse 2 Id
        /// </summary>
        public int? Verse2Id { get; set; }

        /// <summary>
        /// Verse 2
        /// </summary>
        public virtual GanjoorVerse Verse2 { get; set; }

        /// <summary>
        /// note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
