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
        public GanjoorPoemTranslation Language { get; set; }

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
        /// published
        /// </summary>
        public bool Published { get; set; }
    }
}
