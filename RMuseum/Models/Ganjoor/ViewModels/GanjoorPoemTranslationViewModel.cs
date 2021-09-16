namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// poem translation view model
    /// </summary>
    public class GanjoorPoemTranslationViewModel
    {
        /// <summary>
        /// language
        /// </summary>
        public GanjoorLanguage Language { get; set; }

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
        /// comments or description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// contributer name
        /// </summary>
        public string ContributerName { get; set; }

        /// <summary>
        /// translated verses
        /// </summary>
        public GanjoorVerseTranslationViewModel[] TranslatedVerses { get; set; }
    }
}
