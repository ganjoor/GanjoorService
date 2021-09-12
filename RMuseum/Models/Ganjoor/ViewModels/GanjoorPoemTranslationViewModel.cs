namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// poem translation view model
    /// </summary>
    public class GanjoorPoemTranslationViewModel
    {
        /// <summary>
        /// language id
        /// </summary>
        public int LanguageId { get; set; }
        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// translated title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// translated verses
        /// </summary>
        public GanjoorVerseTranslationViewModel[] TranslatedVerses { get; set; }
    }
}
