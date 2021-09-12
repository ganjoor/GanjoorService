namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Verse translation
    /// </summary>
    public class GanjoorVerseTranslation
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// verse id
        /// </summary>
        public int VerseId { get; set; }

        /// <summary>
        /// verse
        /// </summary>
        public GanjoorVerse Verse { get; set; }

        /// <summary>
        /// translated text
        /// </summary>
        public string TText { get; set; }
    }
}
