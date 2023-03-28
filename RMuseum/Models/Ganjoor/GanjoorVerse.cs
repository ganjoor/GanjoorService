namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Verse
    /// </summary>
    public class GanjoorVerse
    {
        /// <summary>
        /// global id, auto generated (missing in Ganjoor Desktop database)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem_id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// vorder
        /// </summary>
        public int VOrder { get; set; }

        /// <summary>
        /// position
        /// </summary>
        public VersePosition VersePosition { get; set; }

        /// <summary>
        /// text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int? CoupletIndex { get; set; }

        /// <summary>
        /// GanjoorPoemSection index
        /// </summary>
        public int? SectionIndex1 { get; set; }

        /// <summary>
        /// second GanjoorPoemSection index
        /// </summary>
        public int? SectionIndex2 { get; set; }

        /// <summary>
        /// third GanjoorPoemSection index
        /// </summary>
        public int? SectionIndex3 { get; set; }

        /// <summary>
        /// forth GanjoorPoemSection index
        /// </summary>
        public int? SectionIndex4 { get; set; }

        /// <summary>
        /// language id
        /// </summary>
        public int? LanguageId { get; set; }

        /// <summary>
        /// language
        /// </summary>
        public virtual GanjoorLanguage Language { get; set; }

        /// <summary>
        /// couplet summary
        /// </summary>
        public string CoupletSummary { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
