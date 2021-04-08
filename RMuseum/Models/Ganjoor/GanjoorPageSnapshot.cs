using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// snapshot of ganjoor pages to keep track of their changes
    /// </summary>
    public class GanjoorPageSnapshot
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ganjoor page id
        /// </summary>
        public int GanjoorPageId { get; set; }

        /// <summary>
        /// ganjoor page
        /// </summary>
        public GanjoorPage GanjoorPage { get; set; }

        /// <summary>
        /// id - this record is then modified by this user and made obsolete
        /// </summary>
        public Guid MadeObsoleteByUserId { get; set; }

        /// <summary>
        /// this record is then modified by this user and made obsolete
        /// </summary>
        public RAppUser MadeObsoleteByUser { get; set; }

        /// <summary>
        /// record date
        /// </summary>
        public DateTime RecordDate { get; set; }

        /// <summary>
        /// a description of the modfication
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// Html Text
        /// </summary>
        public string HtmlText { get; set; }

        /// <summary>
        /// Poem Rhythm
        /// </summary>
        public string Rhythm { get; set; }

        /// <summary>
        /// rhyme letters
        /// </summary>
        public string RhymeLetters { get; set; }

        /// <summary>
        /// source name
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// source url slug
        /// </summary>
        public string SourceUrlSlug { get; set; }

        /// <summary>
        /// old collection or book name for Saadi's ghazalyiat (طیبات، خواتیم و ....)
        /// </summary>
        public string OldTag { get; set; }

        /// <summary>
        /// old collection page url e.g /saadi/tayyebat
        /// </summary>
        public string OldTagPageUrl { get; set; }
    }
}
