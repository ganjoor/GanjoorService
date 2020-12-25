using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration.ViewModels;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// a more complete GanjoorPoem View Model
    /// </summary>
    public class GanjoorPoemCompleteViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

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
        /// category
        /// </summary>
        public GanjoorPoetCompleteViewModel Category { get; set; }

        /// <summary>
        /// Next Poem
        /// </summary>
        public GanjoorPoemSummaryViewModel Next { get; set; }

        /// <summary>
        /// Previous Poem
        /// </summary>
        public GanjoorPoemSummaryViewModel Previous { get; set; }

        /// <summary>
        /// verses
        /// </summary>
        public GanjoorVerseViewModel[] Verses { get; set; }

        /// <summary>
        /// Recitations
        /// </summary>
        public PublicRecitationViewModel[] Recitations { get; set; }

        /// <summary>
        /// Images
        /// </summary>
        public GanjoorLinkViewModel[] Images { get; set; }
    }
}
