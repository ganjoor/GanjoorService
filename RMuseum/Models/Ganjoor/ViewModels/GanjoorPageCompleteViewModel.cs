namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Page View Model
    /// </summary>
    public class GanjoorPageCompleteViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Ganjoor Page Type
        /// </summary>
        public GanjoorPageType GanjoorPageType { get; set; }

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
        /// Html Text
        /// </summary>
        public string HtmlText { get; set; }

        /// <summary>
        /// no index (search engines are blocked to index the page)
        /// </summary>
        public bool NoIndex { get; set; }

        /// <summary>
        /// if a page url is changed, store the old URL here to be redirected automatically
        /// </summary>
        public string RedirectFromFullUrl { get; set; }

        /// <summary>
        /// Poet or Cat
        /// </summary>
        public GanjoorPoetCompleteViewModel PoetOrCat { get; set; }

        /// <summary>
        /// Poem
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        /// <summary>
        /// Second Poet
        /// </summary>
        public GanjoorPoetViewModel SecondPoet { get; set; }

        /// <summary>
        /// next normal page
        /// </summary>
        public GanjoorPageSummaryViewModel Next { get; set; }

        /// <summary>
        /// previous normal page
        /// </summary>
        public GanjoorPageSummaryViewModel Previous { get; set; }
    }
}
