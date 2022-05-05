namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// poem summary
    /// </summary>
    public class GanjoorPoemSummaryViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// excerpt text
        /// </summary>
        public string Excerpt { get; set; }

        /// <summary>
        /// whole poem sections
        /// </summary>
        public GanjoorPoemSection[] MainSections { get; set; }

    }
}
