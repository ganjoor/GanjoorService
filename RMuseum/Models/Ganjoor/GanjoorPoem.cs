using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Poem
    /// </summary>
    public class GanjoorPoem
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// cat-id
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// cat
        /// </summary>
        public GanjoorCat Cat { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// cat + parent cats title + title
        /// </summary>
        public string FullTitle { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// sample: /hafez/ghazal/sh1
        /// </summary>
        public string FullUrl { get; set; }

        /// <summary>
        /// verses text
        /// </summary>
        public string PlainText { get; set; }

        /// <summary>
        /// verses text as html (ganjoor.net format)
        /// </summary>
        public string HtmlText { get; set; }

        /// <summary>
        /// prosody information
        /// </summary>
        /// <remarks>
        /// in fact this should be a many-to-many relationship, but our current dataset lacks such a relationship instance and
        /// because in fact it is actually this relationship should exists between a non-existant entity called block of poem <see cref="RMuseum.Models.Ganjoor.GanjoorPoemSection"/>
        /// I ignored this relationship to take care of it whenever the block entity would be added to the data structure
        /// </remarks>
        public int? GanjoorMetreId { get; set; }

        /// <summary>
        /// metre
        /// </summary>
        public virtual GanjoorMetre GanjoorMetre { get; set; }

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

        /// <summary>
        /// order when mixed with categories
        /// </summary>
        public int MixedModeOrder { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// language  (this is intended to affect html page encodings and not determine actuallly accents and ....)
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// poem summary
        /// </summary>
        public string PoemSummary { get; set; }
    }
}
