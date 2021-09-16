using RSecurityBackend.Models.Auth.Db;
using System;
using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem translation
    /// </summary>
    public class GanjoorPoemTranslation
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// language id
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// language
        /// </summary>
        public GanjoorLanguage Language { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// title translation
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// verses
        /// </summary>
        public List<GanjoorVerseTranslation> Verses { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// date/time
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// comments
        /// </summary>
        public string Description { get; set; }
    }
}
