namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Verse View Model
    /// </summary>
    public class GanjoorVerseViewModel
    {
        /// <summary>
        /// global id, auto generated (missing in Ganjoor Desktop database)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// vorder
        /// </summary>
        public int VOrder { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int? CoupletIndex { get; set; }

        /// <summary>
        /// position
        /// </summary>
        public VersePosition VersePosition { get; set; }

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
        /// text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// language id
        /// </summary>
        public int? LanguageId { get; set; }

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
