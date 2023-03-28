using RMuseum.Models.GanjoorAudio.ViewModels;

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
        /// language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// poem summary
        /// </summary>
        public string PoemSummary { get; set; }

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
        public PoemRelatedImage[] Images { get; set; }

        /// <summary>
        /// Songs
        /// </summary>
        public PoemMusicTrackViewModel[] Songs { get; set; }

        /// <summary>
        /// Comments
        /// </summary>
        public GanjoorCommentSummaryViewModel[] Comments { get; set; }

        /// <summary>
        /// poem sections
        /// </summary>
        public GanjoorPoemSection[] Sections { get; set; }

        /// <summary>
        /// geo/date tags
        /// </summary>
        public PoemGeoDateTag[] GeoDateTags { get; set; }

        /// <summary>
        /// section index
        /// </summary>
        public int? SectionIndex { get; set; }
    }
}
