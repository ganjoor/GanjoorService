namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// modify page view model
    /// </summary>
    public class GanjoorModifyPageViewModel
    {
        /// <summary>
        /// title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// url => slug
        /// </summary>
        public string UrlSlug { get; set; }

        /// <summary>
        /// Html Text
        /// </summary>
        public string HtmlText { get; set; }

        /// <summary>
        /// Poem Rhythm
        /// </summary>
        public string Rhythm { get; set; }

        /// <summary>
        /// Second Poem Rhythm
        /// </summary>
        public string Rhythm2 { get; set; }

        /// <summary>
        /// Third Poem Rhythm
        /// </summary>
        public string Rhythm3 { get; set; }

        /// <summary>
        /// rhyme letters
        /// </summary>
        public string RhymeLetters { get; set; }

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
        /// no index (search engines are blocked to index the page)
        /// </summary>
        public bool NoIndex { get; set; }

        /// <summary>
        /// if a page url is changed, store the old URL here to be redirected automatically
        /// </summary>
        public string RedirectFromFullUrl { get; set; }

        /// <summary>
        /// order when mixed with categories
        /// </summary>
        public int MixedModeOrder { get; set; }

        /// <summary>
        /// published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// language (this is intended to affect html page encodings and not determine actuallly accents and ....)
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// TOC Style
        /// </summary>
        public GanjoorTOC TableOfContentsStyle { get; set; }

        /// <summary>
        /// Category Type
        /// </summary>
        public GanjoorCatType CatType { get; set; }

        /// <summary>
        /// additional descripion or note
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// html mode of additional descripion or note
        /// </summary>
        public string DescriptionHtml { get; set; }

        /// <summary>
        /// a description of the modfication
        /// </summary>
        public string Note { get; set; }
    }
}
