using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// where a set of imported literary tags comes from (an academic corpus, a book, ...). Tags point to it
    /// through PoemLiteraryTag.OriginRef == Key, so a new source needs a row here, not new code.
    /// </summary>
    public class LiteraryTagSource
    {
        public int Id { get; set; }

        /// <summary>
        /// the OriginRef value used by the tags of this source (unique)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// short name shown next to a tag (e.g. «پیکره»)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// full title of the work
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// authors, one per line
        /// </summary>
        public string Authors { get; set; }

        /// <summary>
        /// link to the work (DOI, publisher page, ...)
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// paragraphs describing the work and Ganjoor's use of it, separated by blank lines
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// license name (e.g. MIT)
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// full license/permission text, shown as is
        /// </summary>
        public string LicenseText { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
