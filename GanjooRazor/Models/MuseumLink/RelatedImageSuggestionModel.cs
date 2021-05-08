namespace GanjooRazor.Models.MuseumLink
{
    /// <summary>
    /// Pinterest or Intstagram image that is being suggested to contain text of a poem
    /// </summary>
    public class RelatedImageSuggestionModel
    {
        /// <summary>
        /// ganjoor title
        /// </summary>
        public string GanjoorTitle { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// ganjoor url
        /// </summary>
        public string GanjoorUrl { get; set; }

        /// <summary>
        /// pinterest page url
        /// </summary>
        public string PinterestUrl { get; set; }

        /// <summary>
        /// pinterest image url
        /// </summary>
        public string PinterestImageUrl { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string AltText { get; set; }

     
    }
}
